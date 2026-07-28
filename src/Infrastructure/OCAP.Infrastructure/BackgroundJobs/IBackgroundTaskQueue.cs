using System.Threading.Channels;

namespace OCAP.Infrastructure.BackgroundJobs;

// Interfaz para la cola de tareas en segundo plano (Background Jobs Foundation).
public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
