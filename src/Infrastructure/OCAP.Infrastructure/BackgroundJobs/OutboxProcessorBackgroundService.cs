using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.BackgroundJobs;

/// <summary>
/// Dispatcher Outbox: publica pendientes al transporte, reintentos, poison → DLQ.
/// </summary>
public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private int _consecutiveFailures;

    public OutboxProcessorBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started.");

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
            var delayTime = TimeSpan.FromSeconds(5);

            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
                _consecutiveFailures = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                var backoffSeconds = Math.Min(60, (int)Math.Pow(2, Math.Min(_consecutiveFailures, 5)) * 5);
                delayTime = TimeSpan.FromSeconds(backoffSeconds);
                _logger.LogWarning(ex, "Outbox Processor transient failure ({FailCount}); retry in {Backoff}s",
                    _consecutiveFailures, backoffSeconds);
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
        var options = scope.ServiceProvider.GetService<IOptions<EventBusOptions>>()?.Value ?? new EventBusOptions();
        var transport = scope.ServiceProvider.GetRequiredService<IEventTransport>();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var dlq = scope.ServiceProvider.GetService<IMessageDeadLetterHandler>();
        var retryPolicy = scope.ServiceProvider.GetService<IMessageRetryPolicy>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        // Legacy outbox table (domain entities) — mark processed when present.
        var legacy = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.Error == null)
            .Take(options.OutboxBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in legacy)
        {
            try
            {
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed legacy outbox {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);
            }
        }

        if (legacy.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var pending = await outboxStore.GetPendingMessagesAsync(options.OutboxBatchSize, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        var batch = pending.Select(m => new RawEventMessage(
            m.Id.ToString("N"),
            m.EventType,
            m.PayloadJson,
            m.Id.ToString("N"),
            m.TenantId,
            Source: "OCAP.Outbox")).ToList();

        try
        {
            if (retryPolicy != null)
            {
                await retryPolicy.ExecuteWithRetryAsync(
                    () => transport.PublishBatchAsync(batch, cancellationToken),
                    cancellationToken);
            }
            else
            {
                await transport.PublishBatchAsync(batch, cancellationToken);
            }

            foreach (var msg in pending)
            {
                await outboxStore.MarkAsProcessedAsync(msg.Id, cancellationToken);
            }

            _logger.LogInformation("Outbox dispatched {Count} messages via {Provider}", pending.Count, transport.ProviderName);
        }
        catch (Exception ex)
        {
            foreach (var msg in pending)
            {
                await outboxStore.MarkAsFailedAsync(msg.Id, ex.Message, cancellationToken);
                var refreshed = await dbContext.DistributedOutboxMessages.FirstOrDefaultAsync(x => x.Id == msg.Id, cancellationToken);
                if (refreshed is { Status: "Failed" } && dlq != null)
                {
                    await dlq.HandleDeadLetterAsync(
                        refreshed.TenantId,
                        refreshed.EventType,
                        refreshed.PayloadJson,
                        ex.Message,
                        refreshed.RetryCount,
                        cancellationToken);
                    _logger.LogWarning("Outbox message {Id} moved to DLQ after poison threshold", refreshed.Id);
                }
            }

            throw;
        }
    }
}
