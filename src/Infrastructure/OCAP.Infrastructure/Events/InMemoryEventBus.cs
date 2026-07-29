using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events;

namespace OCAP.Infrastructure.Events;

// Implementación en memoria del bus de eventos en tiempo real para desarrollo y testing.
// Diseñada para ser reemplazada en producción por proveedores distribuídos (Redis, RabbitMQ, Azure Service Bus).
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Func<IEvent, CancellationToken, Task>>> _handlers = new();
    private readonly ILogger<InMemoryEventBus>? _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus>? logger = null)
    {
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent);
        _logger?.LogDebug("Publicando evento de tipo {EventType} (EventId: {EventId})", eventType.Name, @event.EventId);

        if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.IsEmpty)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error no controlado procesando el evento {EventType} (EventId: {EventId})", eventType.Name, @event.EventId);
            }
        }
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Subscribe<TEvent>((@event, ct) => handler.HandleAsync(@event, ct));
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        var eventType = typeof(TEvent);

        var bag = _handlers.GetOrAdd(eventType, _ => new ConcurrentBag<Func<IEvent, CancellationToken, Task>>());
        bag.Add((ev, ct) => handler((TEvent)ev, ct));
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        // En implementación en memoria, no requiere desuscripción compleja para desarrollo
    }
}
