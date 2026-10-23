using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Application.Services;
using OCAP.Application.UseCases;
using OCAP.Core.Entities;
using OCAP.Core.Events;
using OCAP.Core.Ports;

namespace OCAP.Api.Services;

/// <summary>
/// Mensaje entrante de un canal externo, ya autenticado y con su organización resuelta. <c>UserId</c> es el id
/// estable del contacto en OCAP: cada canal decide cómo derivarlo de su id externo.
/// </summary>
public sealed record InboundChannelMessage(
    Guid TenantId,
    string Channel,
    Guid UserId,
    string ExternalUserId,
    string DisplayName,
    string Text);

public sealed record ChannelReply(string Text, string ProviderUsed);

/// <summary>
/// Atiende un mensaje entrante de cualquier canal: registra al contacto y el mensaje en su conversación, publica el
/// evento para el panel en vivo y obtiene la respuesta del asistente principal. Quien llama envía la respuesta por su
/// canal. Si el asistente falla se responde un mensaje de respaldo: el contacto nunca se queda sin contestación.
/// </summary>
public sealed class ChannelConversationService
{
    private readonly IUserRepository _users;
    private readonly ReceiveMessageUseCase _receiveMessage;
    private readonly IEnterpriseAssistantAgent _assistant;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ChannelConversationService> _logger;

    public ChannelConversationService(
        IUserRepository users,
        ReceiveMessageUseCase receiveMessage,
        IEnterpriseAssistantAgent assistant,
        ILogger<ChannelConversationService> logger,
        IEventBus? eventBus = null)
    {
        _users = users;
        _receiveMessage = receiveMessage;
        _assistant = assistant;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ChannelReply> HandleAsync(InboundChannelMessage message, CancellationToken cancellationToken)
    {
        if (await _users.GetByIdAsync(message.UserId, cancellationToken) is null)
        {
            await _users.SaveAsync(new User(message.UserId, message.DisplayName, message.TenantId), cancellationToken);
        }

        await _receiveMessage.ExecuteAsync(message.UserId, message.Text, message.Channel, cancellationToken);

        if (_eventBus is not null)
        {
            await _eventBus.PublishAsync(
                new MessageReceivedEvent(message.Channel, message.ExternalUserId, message.Text, message.TenantId),
                cancellationToken);
        }

        try
        {
            var result = await _assistant.ProcessRequestAsync(
                new AgentContext(_assistant.GlobalAgentId, message.TenantId, message.UserId, message.Text),
                cancellationToken);
            var text = string.IsNullOrWhiteSpace(result.OutputMessage) ? "He recibido tu mensaje." : result.OutputMessage;
            return new ChannelReply(text, result.ProviderUsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "El asistente no respondió en {Channel}; se usa la respuesta de respaldo.", message.Channel);
            return new ChannelReply(
                "Recibí tu mensaje. En este momento el asistente no está disponible; te responderemos pronto.",
                "fallback");
        }
    }
}
