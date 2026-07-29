namespace OCAP.Core.Events.Distributed;

// Sobre estandarizado para eventos distribuidos con trazabilidad y metadata multi-tenant (CAP-20).
public record EventEnvelope<TPayload>(
    string EventId,
    string CorrelationId,
    string CausationId,
    Guid TenantId,
    Guid UserId,
    DateTime Timestamp,
    int Version,
    string EventType,
    string Source,
    TPayload Payload,
    Dictionary<string, string>? Headers = null,
    Dictionary<string, object>? Metadata = null,
    string? TraceId = null
);
