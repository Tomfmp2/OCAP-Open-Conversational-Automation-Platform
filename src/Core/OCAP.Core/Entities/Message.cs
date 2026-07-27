using OCAP.Core.ValueObjects;

namespace OCAP.Core.Entities;

public enum SenderType
{
    User,
    Agent,
    System
}

public class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageContent Content { get; private set; }
    public SenderType SenderType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Message() { } // EF/ORM Constructor

    public Message(Guid id, Guid conversationId, MessageContent content, SenderType senderType)
    {
        if (id == Guid.Empty) throw new ArgumentException("Message identifier cannot be empty.", nameof(id));
        if (conversationId == Guid.Empty) throw new ArgumentException("Conversation identifier cannot be empty.", nameof(conversationId));
        if (content == null) throw new ArgumentNullException(nameof(content));
        
        Id = id;
        ConversationId = conversationId;
        Content = content;
        SenderType = senderType;
        CreatedAt = DateTime.UtcNow;
    }
}
