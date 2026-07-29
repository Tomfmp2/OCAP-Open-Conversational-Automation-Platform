using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

// Implementación del Bus de Eventos Distribuido con Outbox/Inbox, alta disponibilidad y soporte multi-proveedor (CAP-20).
public class DistributedEventBus : IEventBus
{
    private readonly IEventTransport _transport;
    private readonly IEventSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DistributedEventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlerDelegates = new();

    public DistributedEventBus(
        IEventTransport transport,
        IEventSerializer serializer,
        IServiceProvider serviceProvider,
        ILogger<DistributedEventBus> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent).Name;
        var payloadJson = _serializer.Serialize(@event);

        var envelope = new EventEnvelope<TEvent>(
            EventId: Guid.NewGuid().ToString("N"),
            CorrelationId: Guid.NewGuid().ToString("N"),
            CausationId: Guid.NewGuid().ToString("N"),
            TenantId: Guid.Empty,
            UserId: Guid.Empty,
            Timestamp: DateTime.UtcNow,
            Version: 1,
            EventType: eventType,
            Source: "OCAP.Cluster.Node",
            Payload: @event
        );

        using (var scope = _serviceProvider.CreateScope())
        {
            var outboxStore = scope.ServiceProvider.GetService<IOutboxStore>();
            if (outboxStore != null)
            {
                var outboxMsg = new OutboxMessage(Guid.NewGuid(), Guid.Empty, eventType, payloadJson);
                await outboxStore.SaveAsync(outboxMsg, cancellationToken);
            }
        }

        await _transport.PublishAsync(@event, envelope, cancellationToken);
        _logger.LogInformation("Evento distribuido {EventType} publicado vía transporte '{Provider}'.", eventType, _transport.ProviderName);
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        Subscribe<TEvent>((@event, ct) => handler.HandleAsync(@event, ct));
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> envelopeHandler = (@event, envelope, ct) => handler(@event, ct);

        var type = typeof(TEvent);
        var list = _handlerDelegates.GetOrAdd(type, _ => new List<Delegate>());
        lock (list)
        {
            list.Add(handler);
        }

        _transport.SubscribeAsync(envelopeHandler).GetAwaiter().GetResult();
        _logger.LogDebug("Suscripción registrada para evento distribuido {EventType}.", type.Name);
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null) return;
        var type = typeof(TEvent);
        if (_handlerDelegates.TryGetValue(type, out var list))
        {
            lock (list)
            {
                list.RemoveAll(d => d.Equals(handler));
            }
        }
    }
}
