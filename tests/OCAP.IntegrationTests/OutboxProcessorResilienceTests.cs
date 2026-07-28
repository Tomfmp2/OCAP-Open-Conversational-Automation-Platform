using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Core.Entities;
using OCAP.Infrastructure.BackgroundJobs;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.IntegrationTests;

public class OutboxProcessorResilienceTests
{
    [Fact]
    public async Task OutboxProcessor_ShouldProcessPendingMessagesAndMarkAsProcessed()
    {
        // Arrange
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<OCAPDbContext>(options => options.UseInMemoryDatabase(dbName));

        var serviceProvider = services.BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            var outboxMessage = new OutboxMessage(
                Guid.NewGuid(),
                "UserCreatedEvent",
                "{\"UserId\":\"123\"}"
            );
            dbContext.OutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync();
        }

        var logger = NullLogger<OutboxProcessorBackgroundService>.Instance;
        var processor = new OutboxProcessorBackgroundService(serviceProvider, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(3500)); // Esperar > 3s para pasar el initial delay

        // Act
        var executeTask = processor.StartAsync(cts.Token);
        await Task.Delay(3600);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            var processedMessage = await dbContext.OutboxMessages.FirstOrDefaultAsync();
            Assert.NotNull(processedMessage);
            Assert.NotNull(processedMessage!.ProcessedOnUtc);
        }
    }
}
