namespace OCAP.Core.Events.Distributed;

/// <summary>
/// Mensaje de transporte serializado para outbox dispatcher, batch publish y brokers AMQP/NATS.
/// </summary>
public sealed record RawEventMessage(
    string EventId,
    string EventType,
    string PayloadJson,
    string CorrelationId,
    Guid TenantId,
    string? CausationId = null,
    string? TraceId = null,
    string Source = "OCAP",
    IReadOnlyDictionary<string, string>? Headers = null);
