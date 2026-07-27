using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Mock;

// Implementación de prueba para el envío de mensajes hacia clientes simulados.
// Almacena los mensajes despachados en memoria para verificar respuestas en pruebas unitarias e integración.
public class MockMessageSender : IMessageSender
{
    private readonly ILogger<MockMessageSender> _logger;
    private readonly List<OutgoingChannelMessage> _sentMessages = new();

    public MockMessageSender(ILogger<MockMessageSender> logger)
    {
        _logger = logger;
    }

    // Historial de mensajes despachados por esta instancia (read-only para tests).
    public IReadOnlyList<OutgoingChannelMessage> SentMessages
    {
        get
        {
            lock (_sentMessages)
            {
                return _sentMessages.ToList().AsReadOnly();
            }
        }
    }

    // Simula el envío de una respuesta hacia una plataforma externa.
    public Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            _logger.LogWarning("Se intentó enviar un mensaje saliente nulo en MockMessageSender.");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(message.DestinationUserId))
        {
            _logger.LogWarning("Envío rechazado: DestinationUserId no puede estar vacío.");
            return Task.FromResult(false);
        }

        lock (_sentMessages)
        {
            _sentMessages.Add(message);
        }

        _logger.LogInformation("Mensaje Mock enviado a {Destination} vía canal {Channel}: {Content}",
            message.DestinationUserId, message.ChannelName, message.Message);

        return Task.FromResult(true);
    }

    // Limpia el historial de mensajes salientes registrados en memoria.
    public void Clear()
    {
        lock (_sentMessages)
        {
            _sentMessages.Clear();
        }
    }
}
