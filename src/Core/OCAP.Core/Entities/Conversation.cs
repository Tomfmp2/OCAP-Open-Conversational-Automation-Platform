using OCAP.Core.Events;

namespace OCAP.Core.Entities;

public enum ConversationStatus
{
    Active,
    Paused,
    Closed,
    WaitingHuman
}

public class Conversation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ConversationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private Conversation() { } // EF/ORM

    public Conversation(Guid id, Guid userId)
    {
        if (id == Guid.Empty) throw new ArgumentException("Conversation identifier cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User identifier cannot be empty.", nameof(userId));

        Id = id;
        UserId = userId;
        Status = ConversationStatus.Active;
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        
        AddDomainEvent(new ConversationStartedEvent(Id, UserId, CreatedAt));
    }

    public void Close()
    {
        if (Status == ConversationStatus.Closed) return;
        
        Status = ConversationStatus.Closed;
        LastActivityAt = DateTime.UtcNow;
        AddDomainEvent(new ConversationClosedEvent(Id, LastActivityAt));
    }

    public void Pause()
    {
        if (Status != ConversationStatus.Closed)
        {
            Status = ConversationStatus.Paused;
            LastActivityAt = DateTime.UtcNow;
        }
    }

    public void RequestHumanIntervention()
    {
        if (Status != ConversationStatus.Closed)
        {
            Status = ConversationStatus.WaitingHuman;
            LastActivityAt = DateTime.UtcNow;
            AddDomainEvent(new HumanInterventionRequestedEvent(Id, UserId, LastActivityAt));
        }
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
