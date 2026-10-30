namespace OCAP.Core.Events.Distributed;

// Contrato de persistencia para el patrón Inbox e idempotencia (CAP-20).
public interface IInboxStore
{
    Task<bool> HasBeenProcessedAsync(string messageId, string consumerGroup = "Default", CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string messageId, string consumerGroup = "Default", CancellationToken cancellationToken = default);

    // Registra el mensaje y devuelve false si ya estaba: idempotencia atómica entre réplicas y reinicios (el índice
    // único de MessageId + ConsumerGroup decide cuando dos peticiones llegan a la vez).
    Task<bool> TryClaimAsync(string messageId, string consumerGroup, CancellationToken cancellationToken = default);
}
