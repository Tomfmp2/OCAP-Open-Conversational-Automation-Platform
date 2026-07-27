namespace OCAP.Core.Entities;

// Entidad de Sesión de usuario para auditoría y contexto.
public class Session
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string ContextData { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Constructor privado para hidratación mediante Entity Framework Core.
    private Session()
    {
        ContextData = string.Empty;
    }

    public Session(Guid id, Guid conversationId, TimeSpan duration)
    {
        if (id == Guid.Empty) throw new ArgumentException("Session identifier cannot be empty.", nameof(id));
        if (conversationId == Guid.Empty) throw new ArgumentException("Conversation identifier cannot be empty.", nameof(conversationId));
        if (duration <= TimeSpan.Zero) throw new ArgumentException("Session duration must be greater than zero.", nameof(duration));

        Id = id;
        ConversationId = conversationId;
        ContextData = string.Empty;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(duration);
    }

    public void UpdateContext(string contextData)
    {
        if (IsExpired()) throw new InvalidOperationException("Cannot update context of an expired session.");
        ContextData = contextData ?? string.Empty;
    }

    public void Restart(TimeSpan newDuration)
    {
        if (newDuration <= TimeSpan.Zero) throw new ArgumentException("Session duration must be greater than zero.", nameof(newDuration));
        ExpiresAt = DateTime.UtcNow.Add(newDuration);
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }
}
