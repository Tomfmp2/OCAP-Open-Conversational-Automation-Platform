using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Core.Events;

namespace OCAP.Channels.WhatsApp.Services;

public class WhatsAppMessageSender : IMessageSender
{
    private readonly WhatsAppApiClient _apiClient;
    private readonly ILogger<WhatsAppMessageSender> _logger;
    private readonly IEventBus? _eventBus;

    public WhatsAppMessageSender(
        WhatsAppApiClient apiClient,
        ILogger<WhatsAppMessageSender> logger,
        IEventBus? eventBus = null)
    {
        _apiClient = apiClient;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.DestinationUserId) || string.IsNullOrWhiteSpace(message.Message))
        {
            _logger.LogWarning("Se intentó enviar un OutgoingChannelMessage nulo o inválido en WhatsAppMessageSender.");
            return false;
        }

        try
        {
            var request = new WhatsAppCloudSendMessageRequest
            {
                To = message.DestinationUserId,
                Text = new WhatsAppCloudText { Body = message.Message }
            };

            // Intentar extraer de metadatos si OCAP enruta los secretos en este contexto
            message.Metadata.TryGetValue("PhoneNumberId", out string? phoneNumberId);
            message.Metadata.TryGetValue("ApiToken", out string? overrideToken);

            if (string.IsNullOrWhiteSpace(phoneNumberId))
            {
                _logger.LogError("No se pudo obtener el PhoneNumberId desde Metadata al despachar el mensaje de WhatsApp.");
                return false;
            }

            var success = await _apiClient.SendMessageAsync(phoneNumberId, request, overrideToken, cancellationToken);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new MessageSentEvent("WhatsApp", message.DestinationUserId, message.Message, success, Guid.Empty), cancellationToken);
            }

            if (success)
            {
                _logger.LogInformation("Mensaje de respuesta despachado exitosamente a WhatsApp para To {To}.", message.DestinationUserId);
            }
            else
            {
                _logger.LogWarning("Falló el despacho del mensaje de respuesta a WhatsApp para To {To}.", message.DestinationUserId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado al despachar mensaje WhatsApp a {Destination}.", message.DestinationUserId);
            return false;
        }
    }
}
