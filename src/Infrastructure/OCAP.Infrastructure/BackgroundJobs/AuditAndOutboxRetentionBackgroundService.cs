using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Infrastructure.Options;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.BackgroundJobs;

/// <summary>
/// Servicio en segundo plano para el mantenimiento y la purga programada de AuditLogs y OutboxMessages.
/// Implementa "Global Retention Policy v1.6.0" con eliminación incremental por lotes para evitar bloqueos de tabla.
/// Diseñado para evolucionar en el futuro hacia políticas avanzadas por Tenant ("Tenant Retention Policies").
/// </summary>
public class AuditAndOutboxRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<RetentionOptions> _options;
    private readonly ILogger<AuditAndOutboxRetentionBackgroundService> _logger;

    public AuditAndOutboxRetentionBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<RetentionOptions> options,
        ILogger<AuditAndOutboxRetentionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit and Outbox Retention Background Service started.");

        // Breve retraso inicial para no competir con la inicialización del backend y migraciones
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.Value;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await PerformRetentionPurgeAsync(options, stoppingToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Retention Purge Cycle completed in {DurationMs} ms.",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Error during Retention Purge Cycle after {DurationMs} ms. Will retry in next interval.",
                    stopwatch.ElapsedMilliseconds);
            }

            var intervalHours = Math.Max(1, options.ExecutionIntervalHours);
            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Audit and Outbox Retention Background Service stopped.");
    }

    public async Task PerformRetentionPurgeAsync(RetentionOptions options, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        int totalAuditLogsDeleted = 0;
        int totalOutboxMessagesDeleted = 0;

        // 1. Purga de AuditLogs
        if (options.EnableAuditPurge && options.AuditLogRetentionDays > 0)
        {
            var auditCutoffDate = DateTime.UtcNow.AddDays(-options.AuditLogRetentionDays);
            totalAuditLogsDeleted = await PurgeAuditLogsInBatchesAsync(dbContext, auditCutoffDate, options.BatchSize, cancellationToken);
        }

        // 2. Purga de OutboxMessages Procesados
        if (options.EnableOutboxPurge && options.OutboxRetentionDays > 0)
        {
            var outboxCutoffDate = DateTime.UtcNow.AddDays(-options.OutboxRetentionDays);
            totalOutboxMessagesDeleted = await PurgeOutboxMessagesInBatchesAsync(dbContext, outboxCutoffDate, options.BatchSize, cancellationToken);
        }

        _logger.LogInformation(
            "Retention Summary: Purged {AuditCount} expired AuditLogs (Cutoff: {AuditDays} days) and {OutboxCount} processed OutboxMessages (Cutoff: {OutboxDays} days).",
            totalAuditLogsDeleted,
            options.AuditLogRetentionDays,
            totalOutboxMessagesDeleted,
            options.OutboxRetentionDays);
    }

    private async Task<int> PurgeAuditLogsInBatchesAsync(
        OCAPDbContext dbContext,
        DateTime cutoffDate,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int totalDeleted = 0;
        int currentBatchSize = Math.Max(1, batchSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            var expiredIds = await dbContext.AuditLogs
                .Where(a => a.TimestampUtc < cutoffDate)
                .Select(a => a.Id)
                .Take(currentBatchSize)
                .ToListAsync(cancellationToken);

            if (!expiredIds.Any())
            {
                break;
            }

            int deletedInBatch;
            if (dbContext.Database.IsRelational())
            {
                deletedInBatch = await dbContext.AuditLogs
                    .Where(a => expiredIds.Contains(a.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }
            else
            {
                var itemsToDelete = await dbContext.AuditLogs
                    .Where(a => expiredIds.Contains(a.Id))
                    .ToListAsync(cancellationToken);

                dbContext.AuditLogs.RemoveRange(itemsToDelete);
                deletedInBatch = await dbContext.SaveChangesAsync(cancellationToken);
            }

            totalDeleted += deletedInBatch;

            if (expiredIds.Count < currentBatchSize)
            {
                break; // Último lote procesado
            }
        }

        return totalDeleted;
    }

    private async Task<int> PurgeOutboxMessagesInBatchesAsync(
        OCAPDbContext dbContext,
        DateTime cutoffDate,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int totalDeleted = 0;
        int currentBatchSize = Math.Max(1, batchSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Solo purga mensajes que estén completamente procesados, sin errores y anteriores al periodo de retención
            var eligibleIds = await dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc != null && m.Error == null && m.ProcessedOnUtc < cutoffDate)
                .Select(m => m.Id)
                .Take(currentBatchSize)
                .ToListAsync(cancellationToken);

            if (!eligibleIds.Any())
            {
                break;
            }

            int deletedInBatch;
            if (dbContext.Database.IsRelational())
            {
                deletedInBatch = await dbContext.OutboxMessages
                    .Where(m => eligibleIds.Contains(m.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }
            else
            {
                var itemsToDelete = await dbContext.OutboxMessages
                    .Where(m => eligibleIds.Contains(m.Id))
                    .ToListAsync(cancellationToken);

                dbContext.OutboxMessages.RemoveRange(itemsToDelete);
                deletedInBatch = await dbContext.SaveChangesAsync(cancellationToken);
            }

            totalDeleted += deletedInBatch;

            if (eligibleIds.Count < currentBatchSize)
            {
                break; // Último lote procesado
            }
        }

        return totalDeleted;
    }
}
