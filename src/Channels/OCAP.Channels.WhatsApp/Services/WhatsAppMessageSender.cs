using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.Evolution;

namespace OCAP.Channels.WhatsApp.Services;

// Adaptador para canal WhatsApp que implementa IMessageSender.
// Despacha mensajes salientes generados por OCAP utilizando EvolutionApiClient.
public class WhatsAppMessageSender : IMessageSender
{
    private readonly EvolutionApiClient _apiClient;
    private readonly ILogger<WhatsAppMessageSender> _logger;

    public WhatsAppMessageSender(
        EvolutionApiClient apiClient,
        ILogger<WhatsAppMessageSender> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // Transmite un mensaje saliente hacia Evolution API.
    public async Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.DestinationUserId))
        {
            _logger.LogWarning("Intento de envío con mensaje saliente nulo o destino vacío en WhatsAppMessageSender.");
            return false;
        }

        _logger.LogInformation("Enviando mensaje WhatsApp saliente hacia destinatario {Destination}", message.DestinationUserId);

        var success = await _apiClient.SendTextMessageAsync(message.DestinationUserId, message.Message, cancellationToken);
        
        if (!success)
        {
            _logger.LogError("Falló el envío de respuesta WhatsApp hacia destinatario {Destination}", message.DestinationUserId);
        }

        return success;
    }
}
