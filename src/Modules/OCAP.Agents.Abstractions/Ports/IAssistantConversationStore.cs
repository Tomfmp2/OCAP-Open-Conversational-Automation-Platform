using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Abstractions.Ports;

/// <summary>Conversaciones del asistente del panel, siempre filtradas por su dueño.</summary>
public interface IAssistantConversationStore
{
    Task<IReadOnlyList<AssistantConversation>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AssistantConversation?> FindAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistantMessage>> GetMessagesAsync(Guid conversationId, int? last = null, CancellationToken cancellationToken = default);
    Task AddAsync(AssistantConversation conversation, CancellationToken cancellationToken = default);
    Task AddMessagesAsync(AssistantConversation conversation, IEnumerable<AssistantMessage> messages, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(AssistantConversation conversation, CancellationToken cancellationToken = default);
}
