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

    /// <summary>Metadato de la conexión con el proveedor de IA (configuración de la organización) que usa el canal.</summary>
    public const string AiProviderMetadataKey = "AiProviderConfigId";

    /// <summary>
    /// Si una persona tomó la conversación y nadie del equipo escribe en este tiempo, el agente la retoma: un bot no
    /// queda mudo para siempre porque alguien intervino y se olvidó de devolverla.
    /// </summary>
    public static readonly TimeSpan HumanHandoffTimeout = TimeSpan.FromMinutes(15);

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
        if (!await AcceptsAsync(channelConnectionId, userId, cancellationToken)) return null;

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
            // Último mensaje del equipo (operador o el aviso de traspaso del agente) entre los recientes.
            var lastStaff = (await _messages.GetRecentAsync(conversation.Id, HistoryTurns + 1, cancellationToken))
                .Where(m => m.SenderType is SenderType.Operator or SenderType.Agent)
                .Select(m => (DateTime?)m.CreatedAt)
                .Max();
            if (lastStaff is { } at && DateTime.UtcNow - at < HumanHandoffTimeout)
            {
                _logger.LogInformation("Conversación {ConversationId} atendida por una persona: el agente no responde.", conversation.Id);
                return null;
            }

            conversation.ReturnToAgent();
            await _conversations.SaveAsync(conversation, cancellationToken);
            _logger.LogInformation("Conversación {ConversationId}: nadie del equipo respondió en {Minutes} minutos, el agente la retoma.",
                conversation.Id, HumanHandoffTimeout.TotalMinutes);
        }

        string reply;
        try
        {
            var history = (await _messages.GetRecentAsync(conversation.Id, HistoryTurns + 1, cancellationToken))
                .SkipLast(1) // el último es el mensaje que se está respondiendo
                .Where(m => m.SenderType != SenderType.System)
                .Select(m => new ChannelTurn(m.SenderType == SenderType.User, m.Content.Value))
                .ToList();

            var assignment = await AssignmentAsync(tenantId, channelConnectionId, cancellationToken);
            var result = await _agent.ReplyAsync(
                new ChannelAgentRequest(
                    tenantId,
                    assignment.AgentId,
                    userId,
                    conversation.Id,
                    message.ChannelName,
                    string.IsNullOrWhiteSpace(displayName) ? message.ExternalUserId : displayName,
                    history,
                    message.Message,
                    AiProviderConfigId: assignment.AiProviderConfigId),
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

    /// <summary>
    /// Un canal apagado no responde. Uno en apagado (<see cref="ChannelShutdown"/>) solo sigue los chats en curso: el
    /// contacto escribió hace menos de 5 minutos. Los demás mensajes se ignoran sin guardarse.
    /// </summary>
    private async Task<bool> AcceptsAsync(Guid? channelConnectionId, Guid contactId, CancellationToken cancellationToken)
    {
        if (channelConnectionId is not { } id) return true;
        var connection = await _connections.FindConnectionAsync(id, cancellationToken);
        if (connection is null) return true;
        if (!connection.Enabled)
        {
            _logger.LogInformation("Mensaje ignorado: el canal {ConnectionId} está apagado.", id);
            return false;
        }
        if (!ChannelShutdown.IsShuttingDown(connection.ConfigurationMetadata)) return true;

        var conversation = await _conversations.GetByUserIdAsync(contactId, cancellationToken);
        var ongoing = conversation is not null && ChannelShutdown.IsOngoing(conversation.LastActivityAt, DateTime.UtcNow);
        if (!ongoing) _logger.LogInformation("Mensaje ignorado: el canal {ConnectionId} se está apagando y este chat no estaba en curso.", id);
        return ongoing;
    }

    /// <summary>Agente y proveedor de IA asignados a la conexión del canal; null = los de la organización.</summary>
    private async Task<(Guid? AgentId, Guid? AiProviderConfigId)> AssignmentAsync(
        Guid tenantId,
        Guid? channelConnectionId,
        CancellationToken cancellationToken)
    {
        if (channelConnectionId is not { } id) return (null, null);
        var connection = await _connections.FindConnectionAsync(id, cancellationToken);
        if (connection is null || connection.TenantId != tenantId) return (null, null);

        Guid? Read(string key) =>
            connection.ConfigurationMetadata.TryGetValue(key, out var raw) && Guid.TryParse(raw, out var value) ? value : null;
        return (Read(AgentIdMetadataKey), Read(AiProviderMetadataKey));
    }
}
