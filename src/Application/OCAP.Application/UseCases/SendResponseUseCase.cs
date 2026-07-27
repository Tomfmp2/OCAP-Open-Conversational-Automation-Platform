using OCAP.Core.Entities;
using OCAP.Core.Ports;
using OCAP.Core.ValueObjects;

namespace OCAP.Application.UseCases;

public class SendResponseUseCase
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageSender _messageSender;

    public SendResponseUseCase(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMessageSender messageSender)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _messageSender = messageSender;
    }

    public async Task ExecuteAsync(Guid conversationId, string responseContent, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation with ID {conversationId} not found.");
        }

        if (conversation.Status == ConversationStatus.Closed)
        {
            throw new InvalidOperationException("Cannot send a response to a closed conversation.");
        }

        var content = new MessageContent(responseContent);
        var message = new Message(Guid.NewGuid(), conversationId, content, SenderType.Agent);

        await _messageRepository.SaveAsync(message, cancellationToken);
        await _messageSender.SendMessageAsync(message, cancellationToken);
        
        conversation.UpdateActivity();
        await _conversationRepository.SaveAsync(conversation, cancellationToken);
    }
}
