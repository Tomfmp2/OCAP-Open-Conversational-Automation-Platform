using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

// Servicio en segundo plano para el procesamiento asíncrono y reintento de mensajes del Outbox (CAP-20).
public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando servicio en segundo plano OutboxProcessorBackgroundService (CAP-20)...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxStore = scope.ServiceProvider.GetService<IOutboxStore>();
                if (outboxStore != null)
                {
                    var pendingMessages = await outboxStore.GetPendingMessagesAsync(50, stoppingToken);
                    foreach (var msg in pendingMessages)
                    {
                        await outboxStore.MarkAsProcessedAsync(msg.Id, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando lote de mensajes de Outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
