namespace OCAP.Core.Events;

// Contrato base para todos los eventos del sistema OCAP.
public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    Guid TenantId { get; }
}
