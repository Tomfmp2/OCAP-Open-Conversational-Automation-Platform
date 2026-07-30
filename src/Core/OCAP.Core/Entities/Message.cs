using OCAP.Core.ValueObjects;

namespace OCAP.Core.Entities;

// Tipo de emisor del mensaje conversacional.
public enum SenderType
{
    User,
    Agent,
    System
}

// Entidad Mensaje en el Dominio DDD.
public class Message
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageContent Content { get; private set; }
    public SenderType SenderType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Constructor privado para hidratación mediante Entity Framework Core.
    private Message()
    {
        Content = null!;
    }

    public Message(Guid id, Guid conversationId, MessageContent content, SenderType senderType, Guid tenantId = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Message identifier cannot be empty.", nameof(id));
        if (conversationId == Guid.Empty) throw new ArgumentException("Conversation identifier cannot be empty.", nameof(conversationId));
        if (content == null) throw new ArgumentNullException(nameof(content));
        
        Id = id;
        TenantId = tenantId;
        ConversationId = conversationId;
        Content = content;
        SenderType = senderType;
        CreatedAt = DateTime.UtcNow;
    }
}
