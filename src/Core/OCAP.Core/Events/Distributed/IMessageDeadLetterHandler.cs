namespace OCAP.Core.Events.Distributed;

// Gestor de mensajes muertos (Dead Letter Handler) (CAP-20).
public interface IMessageDeadLetterHandler
{
    Task HandleDeadLetterAsync(Guid tenantId, string eventType, string payloadJson, string reason, int retryCount, CancellationToken cancellationToken = default);
    Task<List<DeadLetterMessage>> GetDeadLettersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ReplayDeadLetterAsync(Guid deadLetterId, CancellationToken cancellationToken = default);
}
