namespace OCAP.Agents.Domain.Entities;

// Entidad de contexto conversacional que mantiene el estado temporal e intención activa durante un diálogo.
public class ConversationContext
{
    public Guid ConversationId { get; private set; }
    public string? CurrentIntent { get; private set; }
    public Dictionary<string, string> PendingParameters { get; private set; } = new();
    public Dictionary<string, object> State { get; private set; } = new();
    public DateTime LastInteractionAt { get; private set; }

    private ConversationContext() { } // Constructor ORM

    public ConversationContext(Guid conversationId)
    {
        if (conversationId == Guid.Empty) throw new ArgumentException("El ID de conversación no puede ser vacío.", nameof(conversationId));
        
        ConversationId = conversationId;
        LastInteractionAt = DateTime.UtcNow;
    }

    public void SetIntent(string intentName)
    {
        CurrentIntent = intentName;
        LastInteractionAt = DateTime.UtcNow;
    }

    public void SetParameter(string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            PendingParameters[key] = value;
            LastInteractionAt = DateTime.UtcNow;
        }
    }

    public void ClearIntent()
    {
        CurrentIntent = null;
        PendingParameters.Clear();
        LastInteractionAt = DateTime.UtcNow;
    }

    public void UpdateState(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            State[key] = value;
            LastInteractionAt = DateTime.UtcNow;
        }
    }
}
