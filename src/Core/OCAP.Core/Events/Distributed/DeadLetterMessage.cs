namespace OCAP.Core.Events.Distributed;

// Entidad para mensajes descartados en Dead Letter Queue (DLQ) tras agotar reintentos (CAP-20).
public class DeadLetterMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string OriginalPayloadJson { get; private set; } = string.Empty;
    public DateTime FailedAtUtc { get; private set; }
    public string FailureReason { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public bool Replayed { get; private set; } = false;

    private DeadLetterMessage() { }

    public DeadLetterMessage(Guid id, Guid tenantId, string eventType, string originalPayloadJson, string failureReason, int retryCount)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TenantId = tenantId;
        EventType = eventType ?? string.Empty;
        OriginalPayloadJson = originalPayloadJson ?? string.Empty;
        FailedAtUtc = DateTime.UtcNow;
        FailureReason = failureReason ?? string.Empty;
        RetryCount = retryCount;
        Replayed = false;
    }

    public void MarkAsReplayed()
    {
        Replayed = true;
    }
}
