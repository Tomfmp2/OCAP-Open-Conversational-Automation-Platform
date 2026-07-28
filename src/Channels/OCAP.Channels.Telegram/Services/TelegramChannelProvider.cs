using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Telegram.Services;

// Proveedor del canal Telegram que implementa IChannelProvider.
// Administra únicamente el estado del puerto, sus metadatos y el ciclo de vida del canal.
public class TelegramChannelProvider : IChannelProvider
{
    private readonly ILogger<TelegramChannelProvider> _logger;
    private readonly TelegramApiClient _apiClient;
    private readonly ChannelMetadata _metadata;

    public ChannelMetadata Metadata => _metadata;
    public IMessageReceiver Receiver { get; }
    public IMessageSender Sender { get; }

    public TelegramChannelProvider(
        ILogger<TelegramChannelProvider> logger,
        TelegramApiClient apiClient,
        TelegramMessageReceiver receiver,
        TelegramMessageSender sender)
    {
        _logger = logger;
        _apiClient = apiClient;
        Receiver = receiver;
        Sender = sender;

        _metadata = new ChannelMetadata
        {
            ChannelId = "channel-telegram",
            ChannelName = "Telegram",
            Version = "1.0.0",
            IsEnabled = false
        };
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Inicializando proveedor del canal Telegram...");

        var isValid = await _apiClient.GetMeAsync(cancellationToken);
        _metadata.IsEnabled = isValid;

        if (isValid)
        {
            _logger.LogInformation("Proveedor del canal Telegram inicializado y verificado correctamente.");
        }
        else
        {
            _logger.LogWarning("No se pudo verificar las credenciales de Telegram API durante la inicialización.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deteniendo proveedor del canal Telegram.");
        _metadata.IsEnabled = false;
        return Task.CompletedTask;
    }
}
