using Microsoft.Extensions.Logging;
using OCAP.Application.UseCases;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Core.Events;

namespace OCAP.Channels.WhatsApp.Services;

// Adaptador para canal WhatsApp que implementa IMessageReceiver.
// Recibe mensajes entrantes del webhook de Evolution API e invoca los casos de uso de la capa Application.
public class WhatsAppMessageReceiver : IMessageReceiver
{
    private readonly ReceiveMessageUseCase _receiveMessageUseCase;
    private readonly ILogger<WhatsAppMessageReceiver> _logger;
    private readonly IEventBus? _eventBus;

    public WhatsAppMessageReceiver(
        ReceiveMessageUseCase receiveMessageUseCase,
        ILogger<WhatsAppMessageReceiver> logger,
        IEventBus? eventBus = null)
    {
        _receiveMessageUseCase = receiveMessageUseCase;
        _logger = logger;
        _eventBus = eventBus;
    }

    // Procesa un mensaje entrante ya convertido a IncomingChannelMessage y delega a la capa Application.
    public async Task<bool> ReceiveMessageAsync(IncomingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.ExternalUserId))
        {
            _logger.LogWarning("Se recibió un mensaje nulo o con ExternalUserId vacío en WhatsAppMessageReceiver.");
            return false;
        }

        try
        {
            // Mapear identificador externo a Guid de forma determinista para la interacción con ReceiveMessageUseCase.
            var userId = ParseOrGenerateGuid(message.ExternalUserId);

            _logger.LogInformation("Entregando mensaje WhatsApp de usuario externo {ExternalUser} (Guid: {UserId}) a ReceiveMessageUseCase.",
                message.ExternalUserId, userId);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new MessageReceivedEvent("WhatsApp", message.ExternalUserId, message.Message ?? string.Empty, Guid.Empty), cancellationToken);
            }

            await _receiveMessageUseCase.ExecuteAsync(userId, message.Message ?? string.Empty, "WhatsApp", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar el mensaje entrante de WhatsApp para el usuario {User}.", message.ExternalUserId);
            return false;
        }
    }

    // Genera un Guid válido a partir de un string de usuario externo de forma determinista.
    private static Guid ParseOrGenerateGuid(string externalUserId)
    {
        if (Guid.TryParse(externalUserId, out var guid))
        {
            return guid;
        }

        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"OCAP_WhatsApp_{externalUserId}"));
        return new Guid(hash);
    }
}
