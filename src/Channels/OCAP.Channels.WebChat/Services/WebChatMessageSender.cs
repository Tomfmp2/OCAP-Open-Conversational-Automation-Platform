using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Core.Events;
using ChannelMessageSender = OCAP.Channels.Abstractions.Contracts.IMessageSender;

namespace OCAP.Channels.WebChat.Services;

/// <summary>
/// Emisor WebChat: no llama a un proveedor externo; publica el evento de salida
/// para SignalR / auditoría. La respuesta HTTP la entrega el controlador.
/// </summary>
public class WebChatMessageSender : ChannelMessageSender
{
    private readonly ILogger<WebChatMessageSender> _logger;
    private readonly IEventBus? _eventBus;

    public WebChatMessageSender(ILogger<WebChatMessageSender> logger, IEventBus? eventBus = null)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.DestinationUserId) || string.IsNullOrWhiteSpace(message.Message))
        {
            _logger.LogWarning("OutgoingChannelMessage inválido en WebChatMessageSender.");
            return false;
        }

        var tenantId = Guid.Empty;
        if (message.Metadata.TryGetValue("TenantId", out var tenantRaw) && Guid.TryParse(tenantRaw, out var parsedTenant))
        {
            tenantId = parsedTenant;
        }

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(
                new MessageSentEvent("WebChat", message.DestinationUserId, message.Message, true, tenantId),
                cancellationToken);
        }

        _logger.LogInformation("Respuesta WebChat publicada para sesión {Session}.", message.DestinationUserId);
        return true;
    }
}
