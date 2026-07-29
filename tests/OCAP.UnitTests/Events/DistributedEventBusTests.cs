using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;
using Xunit;

namespace OCAP.UnitTests.Events;

public record TestDomainEvent(Guid EventId, string Message, Guid TenantId) : IEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}

public class DistributedEventBusTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task PublishAsync_PublishesToTransport_And_SavesToOutbox()
    {
        var services = new ServiceCollection();
        var dbContext = GetInMemoryDbContext();
        services.AddScoped<IOutboxStore>(_ => new EfOutboxStore(dbContext));
        var serviceProvider = services.BuildServiceProvider();

        var transport = new InMemoryEventTransport();
        var serializer = new JsonEventSerializer();
        var loggerMock = new Mock<ILogger<DistributedEventBus>>();

        var bus = new DistributedEventBus(transport, serializer, serviceProvider, loggerMock.Object);

        bool eventHandled = false;
        bus.Subscribe<TestDomainEvent>((@event, ct) =>
        {
            eventHandled = true;
            Assert.Equal("Hello Distributed Event Bus!", @event.Message);
            return Task.CompletedTask;
        });

        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "Hello Distributed Event Bus!", Guid.NewGuid());
        await bus.PublishAsync(domainEvent);

        Assert.True(eventHandled);

        var pendingOutbox = await dbContext.DistributedOutboxMessages.ToListAsync();
        Assert.Single(pendingOutbox);
        Assert.Equal("TestDomainEvent", pendingOutbox[0].EventType);
    }
}
