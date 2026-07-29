using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Core.Entities;
using OCAP.Core.Ports;

namespace OCAP.Infrastructure.Services;

public class CoreMessageSender : OCAP.Core.Ports.IMessageSender
{
    private readonly IEnumerable<IChannelProvider> _channelProviders;
    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<CoreMessageSender> _logger;

    public CoreMessageSender(
        IEnumerable<IChannelProvider> channelProviders,
        IConversationRepository conversationRepository,
        ILogger<CoreMessageSender> logger)
    {
        _channelProviders = channelProviders;
        _conversationRepository = conversationRepository;
        _logger = logger;
    }

    public async Task SendMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId, cancellationToken);
        if (conversation == null)
        {
            _logger.LogWarning("No se encontró la conversación {ConversationId} para el mensaje {MessageId}.", message.ConversationId, message.Id);
            return;
        }

        var outMessage = new OutgoingChannelMessage
        {
            DestinationUserId = conversation.UserId.ToString(),
            Message = message.Content.Value,
            SentAt = DateTime.UtcNow
        };

        foreach (var provider in _channelProviders)
        {
            try
            {
                outMessage.ChannelName = provider.Metadata.ChannelName;
                bool success = await provider.Sender.SendMessageAsync(outMessage, cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Mensaje {MessageId} enviado exitosamente por el canal {ChannelName}.", message.Id, provider.Metadata.ChannelName);
                    // Usually you'd stop after first successful send if you just want to reach the user, 
                    // or send to a specific channel. For now we broadcast or just log.
                    break; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el mensaje {MessageId} por el canal {ChannelName}.", message.Id, provider.Metadata.ChannelName);
            }
        }
    }
}
