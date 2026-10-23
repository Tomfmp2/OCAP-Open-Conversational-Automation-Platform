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
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public ConversationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    /// <summary>Canal por el que escribe el contacto (WhatsApp, Telegram…); vacío en conversaciones internas.</summary>
    public string? Channel { get; private set; }
    /// <summary>Id del contacto en su canal (número, chat id): a dónde se envía una respuesta del operador.</summary>
    public string? ExternalContactId { get; private set; }
    /// <summary>Conexión del canal que recibió la conversación (bot, número vinculado).</summary>
    public Guid? ChannelConnectionId { get; private set; }

    /// <summary>Mientras una persona atiende la conversación, el agente no responde.</summary>
    public bool IsHandledByHuman => Status == ConversationStatus.WaitingHuman;

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private Conversation() { } // EF/ORM

    public Conversation(Guid id, Guid userId, Guid tenantId = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Conversation identifier cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User identifier cannot be empty.", nameof(userId));

        Id = id;
        TenantId = tenantId;
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

    /// <summary>Registra por dónde llega la conversación, para poder contestar desde el panel.</summary>
    public void AttachChannel(string channel, string externalContactId, Guid? channelConnectionId)
    {
        if (string.IsNullOrWhiteSpace(channel)) throw new ArgumentException("El canal es obligatorio.", nameof(channel));
        if (string.IsNullOrWhiteSpace(externalContactId)) throw new ArgumentException("El contacto es obligatorio.", nameof(externalContactId));
        Channel = channel;
        ExternalContactId = externalContactId;
        ChannelConnectionId = channelConnectionId;
    }

    /// <summary>Una persona toma la conversación: el agente deja de responder hasta <see cref="ReturnToAgent"/>.</summary>
    public void TakeOver() => RequestHumanIntervention();

    public void ReturnToAgent()
    {
        if (Status is ConversationStatus.WaitingHuman or ConversationStatus.Paused)
        {
            Status = ConversationStatus.Active;
            LastActivityAt = DateTime.UtcNow;
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
