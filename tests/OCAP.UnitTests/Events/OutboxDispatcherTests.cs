using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.BackgroundJobs;
using OCAP.Infrastructure.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;
using Xunit;

namespace OCAP.UnitTests.Events;

public class OutboxDispatcherTests
{
    private sealed record SampleEvent(Guid EventId, Guid TenantId, string Name) : IEvent
    {
        public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
    }

    [Fact]
    public async Task OutboxProcessor_PublishesPendingMessages_ToTransport()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<OCAPDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IOutboxStore, EfOutboxStore>();
        services.AddScoped<IMessageDeadLetterHandler, MessageDeadLetterHandler>();
        services.Configure<EventBusOptions>(o =>
        {
            o.Provider = "InMemory";
            o.OutboxBatchSize = 10;
            o.MaxRetries = 3;
        });

        var serializer = new JsonEventSerializer();
        var transport = new InMemoryEventTransport(serializer);
        services.AddSingleton<IEventTransport>(transport);
        services.AddSingleton<IEventSerializer>(serializer);
        services.AddSingleton<IMessageRetryPolicy, ExponentialBackoffRetryPolicy>();

        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            await store.SaveAsync(new OutboxMessage(
                Guid.NewGuid(),
                Guid.NewGuid(),
                nameof(SampleEvent),
                serializer.Serialize(new SampleEvent(Guid.NewGuid(), Guid.NewGuid(), "payload"))), CancellationToken.None);
        }

        RawEventMessage? published = null;
        // Subscribe via PublishRaw path by temporarily wrapping — publish batch goes to transport
        var capturing = new CapturingTransport(msg => published = msg);
        var sp = new ServiceCollection();
        sp.AddLogging();
        sp.AddDbContext<OCAPDbContext>(o => o.UseInMemoryDatabase(dbName));
        sp.AddScoped<IOutboxStore, EfOutboxStore>();
        sp.AddScoped<IMessageDeadLetterHandler, MessageDeadLetterHandler>();
        sp.AddSingleton<IEventTransport>(capturing);
        sp.Configure<EventBusOptions>(o => o.OutboxBatchSize = 10);
        sp.AddSingleton<IMessageRetryPolicy, ExponentialBackoffRetryPolicy>();
        var rebuilt = sp.BuildServiceProvider();

        var logger = Mock.Of<ILogger<OutboxProcessorBackgroundService>>();
        var processor = new OutboxProcessorBackgroundService(rebuilt, logger);

        var method = typeof(OutboxProcessorBackgroundService)
            .GetMethod("ProcessOutboxMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method!.Invoke(processor, new object[] { CancellationToken.None })!;

        Assert.NotNull(published);
        Assert.Equal(nameof(SampleEvent), published!.EventType);

        using var verifyScope = rebuilt.CreateScope();
        var pending = await verifyScope.ServiceProvider.GetRequiredService<IOutboxStore>()
            .GetPendingMessagesAsync(10);
        Assert.Empty(pending);
    }

    private sealed class CapturingTransport : IEventTransport
    {
        private readonly Action<RawEventMessage> _onPublish;
        public CapturingTransport(Action<RawEventMessage> onPublish) => _onPublish = onPublish;
        public string ProviderName => "Capture";
        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task PublishAsync<TEvent>(TEvent @event, EventEnvelope<TEvent> envelope, CancellationToken cancellationToken = default) where TEvent : IEvent
            => PublishRawAsync(new RawEventMessage(envelope.EventId, envelope.EventType, "{}", envelope.CorrelationId, envelope.TenantId), cancellationToken);
        public Task PublishRawAsync(RawEventMessage message, CancellationToken cancellationToken = default)
        {
            _onPublish(message);
            return Task.CompletedTask;
        }
        public async Task PublishBatchAsync(IReadOnlyList<RawEventMessage> messages, CancellationToken cancellationToken = default)
        {
            foreach (var m in messages) await PublishRawAsync(m, cancellationToken);
        }
        public Task SubscribeAsync<TEvent>(Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IEvent
            => Task.CompletedTask;
    }
}
