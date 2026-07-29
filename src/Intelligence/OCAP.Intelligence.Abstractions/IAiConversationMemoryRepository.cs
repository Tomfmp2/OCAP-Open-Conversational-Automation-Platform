using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Abstractions;

public interface IAiConversationMemoryRepository
{
    Task SaveAsync(AiConversationMemory memory, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiConversationMemory>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
