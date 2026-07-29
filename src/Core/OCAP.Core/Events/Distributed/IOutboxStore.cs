namespace OCAP.Core.Events.Distributed;

// Contrato de persistencia para el patrón Outbox (CAP-20).
public interface IOutboxStore
{
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}
