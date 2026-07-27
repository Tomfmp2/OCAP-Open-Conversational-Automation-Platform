using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Mock;

// Proveedor de canal simulado (Mock) para pruebas de desarrollo e integración de OCAP.
// Administra el ciclo de vida del canal y expone componentes de envío y recepción sin APIs externas.
public class MockChannelProvider : IChannelProvider
{
    private readonly ILogger<MockChannelProvider> _logger;
    private readonly MockMessageReceiver _receiver;
    private readonly MockMessageSender _sender;

    public MockChannelProvider(
        ILogger<MockChannelProvider> logger,
        MockMessageReceiver receiver,
        MockMessageSender sender)
    {
        _logger = logger;
        _receiver = receiver;
        _sender = sender;

        Metadata = new ChannelMetadata
        {
            ChannelId = "channel-mock-01",
            ChannelName = "Mock",
            Version = "1.0.0",
            IsEnabled = false
        };
    }

    public ChannelMetadata Metadata { get; private set; }

    public IMessageSender Sender => _sender;

    public IMessageReceiver Receiver => _receiver;

    // Inicializa el canal simulado activando sus metadatos.
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Metadata.IsEnabled = true;
        _logger.LogInformation("Canal Mock inicializado correctamente. Estado: Activo.");
        return Task.CompletedTask;
    }

    // Detiene de forma segura el canal simulado marcándolo como inactivo.
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Metadata.IsEnabled = false;
        _logger.LogInformation("Canal Mock detenido correctamente. Estado: Inactivo.");
        return Task.CompletedTask;
    }
}
