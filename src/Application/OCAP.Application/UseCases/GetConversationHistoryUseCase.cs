using OCAP.Core.Entities;
using OCAP.Core.Ports;

namespace OCAP.Application.UseCases;

public class GetConversationHistoryUseCase
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;

    public GetConversationHistoryUseCase(
        IConversationRepository conversationRepository, 
        IMessageRepository messageRepository)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<IEnumerable<Message>> ExecuteAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation with ID {conversationId} not found.");
        }

        var messages = await _messageRepository.GetByConversationIdAsync(conversationId, cancellationToken);
        return messages.OrderBy(m => m.CreatedAt);
    }
}
