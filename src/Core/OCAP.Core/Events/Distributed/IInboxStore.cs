namespace OCAP.Core.Events.Distributed;

// Contrato de persistencia para el patrón Inbox e idempotencia (CAP-20).
public interface IInboxStore
{
    Task<bool> HasBeenProcessedAsync(string messageId, string consumerGroup = "Default", CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string messageId, string consumerGroup = "Default", CancellationToken cancellationToken = default);
}
