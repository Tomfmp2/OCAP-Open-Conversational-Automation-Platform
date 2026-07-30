namespace OCAP.Core.Entities;

// Entidad para el Patrón Outbox
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { } // Para el ORM

    public OutboxMessage(Guid id, string type, string content, Guid tenantId = default)
    {
        Id = id;
        TenantId = tenantId;
        Type = type;
        Content = content;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }
}
