using System.Security.Cryptography;
using System.Text;
using OCAP.Agents.Application.Services;
using OCAP.Application.UseCases;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Core.Entities;
using OCAP.Core.Events;
using OCAP.Core.Ports;

namespace OCAP.Api.Services;

/// <summary>
/// Atiende un mensaje entrante de cualquier canal: registra al contacto y el mensaje en su conversación (con su
/// canal, para poder contestar desde el panel), publica el evento para el panel en vivo y responde con el agente
/// asignado a la conexión. Si una persona del equipo tomó la conversación, el agente no responde. Si el agente
/// falla, se responde un mensaje de respaldo: el contacto nunca se queda sin contestación.
/// </summary>
public sealed class ChannelConversationService : IConversationResponder
{
    /// <summary>Metadato de la conexión del canal con el agente que la atiende.</summary>
    public const string AgentIdMetadataKey = "AgentId";

    /// <summary>Turnos previos que se le pasan al agente como contexto.</summary>
    private const int HistoryTurns = 12;

    private const string FallbackReply =
        "Recibí tu mensaje. En este momento el asistente no está disponible; te responderemos pronto.";

    private readonly IUserRepository _users;
    private readonly ReceiveMessageUseCase _receiveMessage;
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IChannelConnectionManager _connections;
    private readonly ChannelAgentRunner _agent;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ChannelConversationService> _logger;

    public ChannelConversationService(
        IUserRepository users,
        ReceiveMessageUseCase receiveMessage,
        IConversationRepository conversations,
        IMessageRepository messages,
        IChannelConnectionManager connections,
        ChannelAgentRunner agent,
        ILogger<ChannelConversationService> logger,
        IEventBus? eventBus = null)
    {
        _users = users;
        _receiveMessage = receiveMessage;
        _conversations = conversations;
        _messages = messages;
        _connections = connections;
        _agent = agent;
        _logger = logger;
        _eventBus = eventBus;
    }

    /// <summary>Contacto estable por canal, organización e id externo: el mismo número en otra organización es otro contacto.</summary>
    public static Guid ContactUserId(string channel, Guid tenantId, string externalUserId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{channel.ToLowerInvariant()}:{tenantId:N}:{externalUserId}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    public async Task<string?> RespondAsync(
        Guid tenantId,
        Guid? channelConnectionId,
        IncomingChannelMessage message,
        string displayName,
        CancellationToken cancellationToken)
    {
        var userId = ContactUserId(message.ChannelName, tenantId, message.ExternalUserId);
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
        {
            var name = string.IsNullOrWhiteSpace(displayName) ? message.ExternalUserId : displayName;
            await _users.SaveAsync(new User(userId, name, tenantId), cancellationToken);
        }

        var conversation = await _receiveMessage.ReceiveAsync(
            userId, message.Message, message.ChannelName, message.ExternalUserId, channelConnectionId, cancellationToken);

        if (_eventBus is not null)
        {
            await _eventBus.PublishAsync(
                new MessageReceivedEvent(message.ChannelName, message.ExternalUserId, message.Message, tenantId),
                cancellationToken);
        }

        if (conversation.IsHandledByHuman)
        {
            _logger.LogInformation("Conversación {ConversationId} atendida por una persona: el agente no responde.", conversation.Id);
            return null;
        }

        string reply;
        try
        {
            var history = (await _messages.GetRecentAsync(conversation.Id, HistoryTurns + 1, cancellationToken))
                .SkipLast(1) // el último es el mensaje que se está respondiendo
                .Where(m => m.SenderType != SenderType.System)
                .Select(m => new ChannelTurn(m.SenderType == SenderType.User, m.Content.Value))
                .ToList();

            var result = await _agent.ReplyAsync(
                new ChannelAgentRequest(
                    tenantId,
                    await AgentForAsync(tenantId, channelConnectionId, cancellationToken),
                    userId,
                    conversation.Id,
                    message.ChannelName,
                    string.IsNullOrWhiteSpace(displayName) ? message.ExternalUserId : displayName,
                    history,
                    message.Message),
                cancellationToken);
            reply = result.Text;

            if (result.TransferToHuman)
            {
                conversation.TakeOver();
                await _conversations.SaveAsync(conversation, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "El agente no respondió en {Channel}; se usa la respuesta de respaldo.", message.ChannelName);
            reply = FallbackReply;
        }

        await _receiveMessage.RecordReplyAsync(conversation.Id, reply, SenderType.Agent, cancellationToken);
        return reply;
    }

    /// <summary>Agente asignado a la conexión del canal; null si no tiene (se usa el de atención por defecto).</summary>
    private async Task<Guid?> AgentForAsync(Guid tenantId, Guid? channelConnectionId, CancellationToken cancellationToken)
    {
        if (channelConnectionId is not { } id) return null;
        var connection = await _connections.FindConnectionAsync(id, cancellationToken);
        return connection is not null
               && connection.TenantId == tenantId
               && connection.ConfigurationMetadata.TryGetValue(AgentIdMetadataKey, out var raw)
               && Guid.TryParse(raw, out var agentId)
            ? agentId
            : null;
    }
}
