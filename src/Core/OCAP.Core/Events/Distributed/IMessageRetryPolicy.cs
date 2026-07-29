namespace OCAP.Core.Events.Distributed;

// Política de reintentos con backoff exponencial para mensajes fallidos (CAP-20).
public interface IMessageRetryPolicy
{
    Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
