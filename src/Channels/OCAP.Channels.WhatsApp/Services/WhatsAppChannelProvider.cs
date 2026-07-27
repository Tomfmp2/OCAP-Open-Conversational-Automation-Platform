using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Services;

// Proveedor de canal principal para la integración oficial de WhatsApp vía Evolution API.
// Implementa IChannelProvider para gestionar el ciclo de vida y exponer receptores/emisores.
public class WhatsAppChannelProvider : IChannelProvider
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppChannelProvider> _logger;
    private readonly WhatsAppMessageReceiver _receiver;
    private readonly WhatsAppMessageSender _sender;

    public WhatsAppChannelProvider(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppChannelProvider> logger,
        WhatsAppMessageReceiver receiver,
        WhatsAppMessageSender sender)
    {
        _settings = settings.Value;
        _logger = logger;
        _receiver = receiver;
        _sender = sender;

        Metadata = new ChannelMetadata
        {
            ChannelId = "channel-whatsapp-evolution",
            ChannelName = "WhatsApp",
            Version = "1.0.0",
            IsEnabled = _settings.Enabled
        };
    }

    public ChannelMetadata Metadata { get; private set; }

    public IMessageSender Sender => _sender;

    public IMessageReceiver Receiver => _receiver;

    // Inicializa la configuración e instancia del canal de WhatsApp.
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Inicialización omitida: el canal de WhatsApp está deshabilitado en configuración.");
            Metadata.IsEnabled = false;
            return Task.CompletedTask;
        }

        Metadata.IsEnabled = true;
        _logger.LogInformation("Canal de WhatsApp (Evolution API) inicializado correctamente. Instancia: {Instance}", _settings.Instance);
        return Task.CompletedTask;
    }

    // Detiene el canal de WhatsApp marcando su estado como inactivo.
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Metadata.IsEnabled = false;
        _logger.LogInformation("Canal de WhatsApp detenido.");
        return Task.CompletedTask;
    }
}
