using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.BackgroundJobs;

// Servicio en segundo plano para procesar el Patrón Outbox con Resiliencia y Backoff Progresivo
public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private int _consecutiveFailures = 0;

    public OutboxProcessorBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started.");

        // Breve espera inicial para permitir que el Host y las migraciones de DB terminen de arrancar
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delayTime = TimeSpan.FromSeconds(10);

            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
                _consecutiveFailures = 0; // Reset al tener éxito
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                // Resiliencia con backoff exponencial: 10s -> 20s -> 40s -> max 60s
                var backoffSeconds = Math.Min(60, (int)Math.Pow(2, Math.Min(_consecutiveFailures, 5)) * 5);
                delayTime = TimeSpan.FromSeconds(backoffSeconds);

                if (_consecutiveFailures <= 3)
                {
                    _logger.LogWarning("Outbox Processor: La base de datos o el servicio no está disponible temporalmente (Intento {FailCount}). Reintentando en {Backoff}s. Detalle: {Message}",
                        _consecutiveFailures, backoffSeconds, ex.Message);
                }
                else
                {
                    _logger.LogError(ex, "Outbox Processor: Error persistente procesando mensajes outbox tras {FailCount} fallos consecutivos. Reintentando en {Backoff}s.",
                        _consecutiveFailures, backoffSeconds);
                }
            }

            try
            {
                await Task.Delay(delayTime, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox Processor stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.Error == null)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation("Publishing Outbox Message {MessageId} of type {MessageType}", message.Id, message.Type);
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Outbox Message {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var outboxStore = scope.ServiceProvider.GetService<OCAP.Core.Events.Distributed.IOutboxStore>();
        if (outboxStore != null)
        {
            var pendingMessages = await outboxStore.GetPendingMessagesAsync(20, cancellationToken);
            foreach (var msg in pendingMessages)
            {
                await outboxStore.MarkAsProcessedAsync(msg.Id, cancellationToken);
            }
        }
    }
}

