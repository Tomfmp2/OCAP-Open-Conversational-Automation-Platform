using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.WebChat.Services;

public class WebChatChannelProvider : IChannelProvider
{
    private readonly ILogger<WebChatChannelProvider> _logger;
    private readonly ChannelMetadata _metadata;

    public ChannelMetadata Metadata => _metadata;
    public IMessageReceiver Receiver { get; }
    public IMessageSender Sender { get; }

    public WebChatChannelProvider(
        ILogger<WebChatChannelProvider> logger,
        WebChatMessageReceiver receiver,
        WebChatMessageSender sender)
    {
        _logger = logger;
        Receiver = receiver;
        Sender = sender;
        _metadata = new ChannelMetadata
        {
            ChannelId = "channel-webchat",
            ChannelName = "WebChat",
            Version = "1.0.0",
            IsEnabled = false
        };
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Inicializando proveedor WebChat.");
        _metadata.IsEnabled = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deteniendo proveedor WebChat.");
        _metadata.IsEnabled = false;
        return Task.CompletedTask;
    }
}
