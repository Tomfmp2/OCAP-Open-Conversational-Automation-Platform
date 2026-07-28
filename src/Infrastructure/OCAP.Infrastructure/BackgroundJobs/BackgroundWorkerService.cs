using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OCAP.Infrastructure.BackgroundJobs;

// Background worker (IHostedService) que procesa la cola
public class BackgroundWorkerService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BackgroundWorkerService> _logger;

    public BackgroundWorkerService(IBackgroundTaskQueue taskQueue, ILogger<BackgroundWorkerService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Worker Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                // Ejecutamos la tarea en segundo plano
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Ignorar excepción si el token fue cancelado
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background work item.");
            }
        }

        _logger.LogInformation("Background Worker Service stopped.");
    }
}
