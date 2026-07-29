namespace OCAP.Core.Events.Distributed;

// Entidad de mensajes Inbox para idempotencia y deduplicación de eventos distribuidos (CAP-20).
public class InboxMessage
{
    public Guid Id { get; private set; }
    public string MessageId { get; private set; } = string.Empty;
    public string ConsumerGroup { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; private set; }

    private InboxMessage() { }

    public InboxMessage(Guid id, string messageId, string consumerGroup)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        ConsumerGroup = consumerGroup ?? "Default";
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
