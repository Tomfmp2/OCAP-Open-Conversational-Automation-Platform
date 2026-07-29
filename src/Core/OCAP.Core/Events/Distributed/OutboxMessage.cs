namespace OCAP.Core.Events.Distributed;

// Entidad de mensajes Outbox para garantía de entrega At-Least-Once y consistencia transaccional (CAP-20).
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public string Status { get; private set; } = "Pending"; // Pending, Processed, Failed

    private OutboxMessage() { }

    public OutboxMessage(Guid id, Guid tenantId, string eventType, string payloadJson)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TenantId = tenantId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        PayloadJson = payloadJson ?? throw new ArgumentNullException(nameof(payloadJson));
        CreatedAtUtc = DateTime.UtcNow;
        Status = "Pending";
        RetryCount = 0;
    }

    public void MarkAsProcessed()
    {
        Status = "Processed";
        ProcessedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string error)
    {
        RetryCount++;
        ErrorMessage = error;
        if (RetryCount >= 5)
        {
            Status = "Failed";
        }
    }
}
