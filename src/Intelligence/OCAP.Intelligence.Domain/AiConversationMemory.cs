namespace OCAP.Intelligence.Domain;

// Entidad que representa un registro de memoria contextual persistente para una conversación.
public class AiConversationMemory
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string MemoryType { get; private set; } = "ShortTerm";
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private AiConversationMemory() { } // Constructor ORM

    public AiConversationMemory(Guid id, Guid conversationId, string memoryType, string content)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de memoria no puede ser vacío.", nameof(id));
        if (conversationId == Guid.Empty) throw new ArgumentException("El ID de conversación no puede ser vacío.", nameof(conversationId));

        Id = id;
        ConversationId = conversationId;
        MemoryType = string.IsNullOrWhiteSpace(memoryType) ? "ShortTerm" : memoryType.Trim();
        Content = content ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
