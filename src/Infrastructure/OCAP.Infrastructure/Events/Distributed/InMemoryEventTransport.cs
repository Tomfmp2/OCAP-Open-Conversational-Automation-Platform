using System.Collections.Concurrent;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

public class InMemoryEventTransport : IEventTransport
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public string ProviderName => "InMemory";

    public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public async Task PublishAsync<TEvent>(TEvent @event, EventEnvelope<TEvent> envelope, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            List<Delegate> copy;
            lock (list)
            {
                copy = list.ToList();
            }

            foreach (var del in copy)
            {
                if (del is Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> func)
                {
                    await func(@event, envelope, cancellationToken);
                }
            }
        }
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        var type = typeof(TEvent);
        var list = _handlers.GetOrAdd(type, _ => new List<Delegate>());
        lock (list)
        {
            list.Add(handler);
        }
        return Task.CompletedTask;
    }
}
