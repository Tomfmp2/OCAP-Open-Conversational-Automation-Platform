using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Mock;

// Implementación de prueba para la recepción de mensajes sin depender de un proveedor real.
// Registra eventos con ILogger y mantiene un historial en memoria para aserciones de testing.
public class MockMessageReceiver : IMessageReceiver
{
    private readonly ILogger<MockMessageReceiver> _logger;
    private readonly List<IncomingChannelMessage> _receivedMessages = new();

    // Límite de tamaño de mensaje entrante para seguridad (10 KB por mensaje).
    private const int MaxMessageLength = 10 * 1024;

    public MockMessageReceiver(ILogger<MockMessageReceiver> logger)
    {
        _logger = logger;
    }

    // Historial de mensajes recibidos por esta instancia (read-only para tests).
    public IReadOnlyList<IncomingChannelMessage> ReceivedMessages
    {
        get
        {
            lock (_receivedMessages)
            {
                return _receivedMessages.ToList().AsReadOnly();
            }
        }
    }

    // Procesa un mensaje entrante simulado, aplicando validaciones de seguridad básicas.
    public Task<bool> ReceiveMessageAsync(IncomingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            _logger.LogWarning("Se intentó procesar un mensaje nulo en MockMessageReceiver.");
            return Task.FromResult(false);
        }

        // Sanitización básica: prevenir IDs de usuario vacíos o no confiables.
        if (string.IsNullOrWhiteSpace(message.ExternalUserId))
        {
            _logger.LogWarning("Mensaje rechazado: ExternalUserId no puede estar vacío.");
            return Task.FromResult(false);
        }

        // Validación de tamaño máximo permitido para evitar sobrecarga de memoria.
        if (message.Message != null && message.Message.Length > MaxMessageLength)
        {
            _logger.LogWarning("Mensaje rechazado por exceder el tamaño máximo permitido de {MaxBytes} bytes.", MaxMessageLength);
            return Task.FromResult(false);
        }

        // Sanitización del contenido para no confiar a ciegas en strings externos.
        message.Message = message.Message?.Trim() ?? string.Empty;

        lock (_receivedMessages)
        {
            _receivedMessages.Add(message);
        }

        _logger.LogInformation("Mensaje Mock recibido de {User} en canal {Channel}: {Content}",
            message.ExternalUserId, message.ChannelName, message.Message);

        return Task.FromResult(true);
    }

    // Limpia el historial de mensajes registrados en memoria.
    public void Clear()
    {
        lock (_receivedMessages)
        {
            _receivedMessages.Clear();
        }
    }
}
