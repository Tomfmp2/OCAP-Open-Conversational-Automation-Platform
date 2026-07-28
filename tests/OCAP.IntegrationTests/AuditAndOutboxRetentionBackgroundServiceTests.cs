using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OCAP.Core.Entities;
using OCAP.Infrastructure.BackgroundJobs;
using OCAP.Infrastructure.Options;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Domain.Entities;
using Xunit;

namespace OCAP.IntegrationTests;

public class AuditAndOutboxRetentionBackgroundServiceTests
{
    private (IServiceProvider ServiceProvider, OCAPDbContext DbContext) CreateDbContext(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<OCAPDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        return (serviceProvider, dbContext);
    }

    [Fact]
    public async Task PerformRetentionPurgeAsync_DeletesOnlyExpiredAuditLogs()
    {
        // Arrange
        var (serviceProvider, dbContext) = CreateDbContext(Guid.NewGuid().ToString());
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // AuditLog antiguo (40 días atrás) -> Debe borrarse (Retención: 30 días)
        var oldAudit = new AuditLog(Guid.NewGuid(), tenantId, userId, "LOGIN", "Old Login", "127.0.0.1", true);
        SetTimestampUtc(oldAudit, DateTime.UtcNow.AddDays(-40));

        // AuditLog reciente (5 días atrás) -> Debe conservarse
        var recentAudit = new AuditLog(Guid.NewGuid(), tenantId, userId, "LOGOUT", "Recent Logout", "127.0.0.1", true);
        SetTimestampUtc(recentAudit, DateTime.UtcNow.AddDays(-5));

        dbContext.AuditLogs.AddRange(oldAudit, recentAudit);
        await dbContext.SaveChangesAsync();

        var options = Options.Create(new RetentionOptions
        {
            AuditLogRetentionDays = 30,
            EnableAuditPurge = true,
            EnableOutboxPurge = false
        });

        var logger = NullLogger<AuditAndOutboxRetentionBackgroundService>.Instance;
        var service = new AuditAndOutboxRetentionBackgroundService(serviceProvider, options, logger);

        // Act
        await service.PerformRetentionPurgeAsync(options.Value, CancellationToken.None);

        // Assert
        var remainingAuditLogs = await dbContext.AuditLogs.ToListAsync();
        Assert.Single(remainingAuditLogs);
        Assert.Equal(recentAudit.Id, remainingAuditLogs[0].Id);
    }

    [Fact]
    public async Task PerformRetentionPurgeAsync_DeletesOnlyProcessedOutboxMessages_AndPreservesPendingAndFailed()
    {
        // Arrange
        var (serviceProvider, dbContext) = CreateDbContext(Guid.NewGuid().ToString());

        // 1. Mensaje procesado antiguo (10 días atrás) -> Debe borrarse (Retención: 7 días)
        var oldProcessedMessage = new OutboxMessage(Guid.NewGuid(), "OrderCreated", "{}");
        oldProcessedMessage.MarkAsProcessed();
        SetProcessedOnUtc(oldProcessedMessage, DateTime.UtcNow.AddDays(-10));

        // 2. Mensaje procesado reciente (2 días atrás) -> Debe conservarse
        var recentProcessedMessage = new OutboxMessage(Guid.NewGuid(), "OrderUpdated", "{}");
        recentProcessedMessage.MarkAsProcessed();
        SetProcessedOnUtc(recentProcessedMessage, DateTime.UtcNow.AddDays(-2));

        // 3. Mensaje PENDIENTE antiguo (10 días atrás, sin procesar) -> Debe CONSERVARSE (ProcessedOnUtc == null)
        var pendingMessage = new OutboxMessage(Guid.NewGuid(), "PaymentPending", "{}");

        // 4. Mensaje FALLIDO antiguo (10 días atrás, con error) -> Debe CONSERVARSE (Error != null)
        var failedMessage = new OutboxMessage(Guid.NewGuid(), "PaymentFailed", "{}");
        failedMessage.MarkAsFailed("Gateway connection timeout");
        SetProcessedOnUtc(failedMessage, DateTime.UtcNow.AddDays(-10));

        dbContext.OutboxMessages.AddRange(oldProcessedMessage, recentProcessedMessage, pendingMessage, failedMessage);
        await dbContext.SaveChangesAsync();

        var options = Options.Create(new RetentionOptions
        {
            OutboxRetentionDays = 7,
            EnableAuditPurge = false,
            EnableOutboxPurge = true
        });

        var logger = NullLogger<AuditAndOutboxRetentionBackgroundService>.Instance;
        var service = new AuditAndOutboxRetentionBackgroundService(serviceProvider, options, logger);

        // Act
        await service.PerformRetentionPurgeAsync(options.Value, CancellationToken.None);

        // Assert
        var remainingMessages = await dbContext.OutboxMessages.ToListAsync();
        Assert.Equal(3, remainingMessages.Count);
        Assert.Contains(remainingMessages, m => m.Id == recentProcessedMessage.Id);
        Assert.Contains(remainingMessages, m => m.Id == pendingMessage.Id);
        Assert.Contains(remainingMessages, m => m.Id == failedMessage.Id);
        Assert.DoesNotContain(remainingMessages, m => m.Id == oldProcessedMessage.Id);
    }

    [Fact]
    public async Task PerformRetentionPurgeAsync_RespectsDisabledOptions()
    {
        // Arrange
        var (serviceProvider, dbContext) = CreateDbContext(Guid.NewGuid().ToString());

        var oldAudit = new AuditLog(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ACTION", "Details", "127.0.0.1", true);
        SetTimestampUtc(oldAudit, DateTime.UtcNow.AddDays(-100));

        var oldProcessedMessage = new OutboxMessage(Guid.NewGuid(), "TestEvent", "{}");
        oldProcessedMessage.MarkAsProcessed();
        SetProcessedOnUtc(oldProcessedMessage, DateTime.UtcNow.AddDays(-100));

        dbContext.AuditLogs.Add(oldAudit);
        dbContext.OutboxMessages.Add(oldProcessedMessage);
        await dbContext.SaveChangesAsync();

        var options = Options.Create(new RetentionOptions
        {
            EnableAuditPurge = false,
            EnableOutboxPurge = false
        });

        var logger = NullLogger<AuditAndOutboxRetentionBackgroundService>.Instance;
        var service = new AuditAndOutboxRetentionBackgroundService(serviceProvider, options, logger);

        // Act
        await service.PerformRetentionPurgeAsync(options.Value, CancellationToken.None);

        // Assert
        Assert.Equal(1, await dbContext.AuditLogs.CountAsync());
        Assert.Equal(1, await dbContext.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task PerformRetentionPurgeAsync_ProcessesInBatches()
    {
        // Arrange
        var (serviceProvider, dbContext) = CreateDbContext(Guid.NewGuid().ToString());
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Agregar 5 registros antiguos con BatchSize = 2
        for (int i = 0; i < 5; i++)
        {
            var audit = new AuditLog(Guid.NewGuid(), tenantId, userId, $"ACTION_{i}", "Batch Audit", "127.0.0.1", true);
            SetTimestampUtc(audit, DateTime.UtcNow.AddDays(-50));
            dbContext.AuditLogs.Add(audit);
        }
        await dbContext.SaveChangesAsync();

        var options = Options.Create(new RetentionOptions
        {
            AuditLogRetentionDays = 30,
            BatchSize = 2,
            EnableAuditPurge = true,
            EnableOutboxPurge = false
        });

        var logger = NullLogger<AuditAndOutboxRetentionBackgroundService>.Instance;
        var service = new AuditAndOutboxRetentionBackgroundService(serviceProvider, options, logger);

        // Act
        await service.PerformRetentionPurgeAsync(options.Value, CancellationToken.None);

        // Assert
        Assert.Equal(0, await dbContext.AuditLogs.CountAsync());
    }

    // Helper mediante reflexión para modificar propiedades privadas con Setters privados
    private static void SetTimestampUtc(AuditLog auditLog, DateTime timestampUtc)
    {
        var property = typeof(AuditLog).GetProperty(nameof(AuditLog.TimestampUtc));
        property?.SetValue(auditLog, timestampUtc);
    }

    private static void SetProcessedOnUtc(OutboxMessage outboxMessage, DateTime processedOnUtc)
    {
        var property = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.ProcessedOnUtc));
        property?.SetValue(outboxMessage, processedOnUtc);
    }
}
