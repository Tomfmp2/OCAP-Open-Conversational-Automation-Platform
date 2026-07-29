using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Services;

public class WhatsAppChannelProvider : IChannelProvider
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppChannelProvider> _logger;
    private readonly WhatsAppMessageReceiver _receiver;
    private readonly WhatsAppMessageSender _sender;
    private readonly ChannelMetadata _metadata;

    public ChannelMetadata Metadata => _metadata;
    public IMessageReceiver Receiver => _receiver;
    public IMessageSender Sender => _sender;

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

        _metadata = new ChannelMetadata
        {
            ChannelId = "channel-whatsapp-cloud",
            ChannelName = "WhatsApp",
            Version = "1.0.0",
            IsEnabled = false
        };
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Inicialización omitida: el canal de WhatsApp Cloud API está deshabilitado en configuración.");
            _metadata.IsEnabled = false;
            return Task.CompletedTask;
        }

        _metadata.IsEnabled = true;
        _logger.LogInformation("Canal de WhatsApp Cloud API inicializado correctamente.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _metadata.IsEnabled = false;
        _logger.LogInformation("Proveedor del canal WhatsApp detenido.");
        return Task.CompletedTask;
    }
}
