using OCAP.Core.Entities;

namespace OCAP.Core.Ports;

public interface IMessageSender
{
    Task SendMessageAsync(Message message, CancellationToken cancellationToken = default);
}

public interface IMessageReceiver
{
    Task ReceiveMessageAsync(Message message, CancellationToken cancellationToken = default);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(User user, CancellationToken cancellationToken = default);
}

public interface IMessageRepository
{
    Task SaveAsync(Message message, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

public interface IToolExecutionRepository
{
    Task SaveAsync(ToolExecution toolExecution, CancellationToken cancellationToken = default);
}
