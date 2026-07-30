using System.Collections.Concurrent;
using System.Text.Json;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

public class InMemoryEventTransport : IEventTransport
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly ConcurrentDictionary<string, List<Func<RawEventMessage, CancellationToken, Task>>> _rawHandlers = new(StringComparer.Ordinal);
    private readonly IEventSerializer? _serializer;

    public string ProviderName => "InMemory";

    public InMemoryEventTransport(IEventSerializer? serializer = null)
    {
        _serializer = serializer;
    }

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

    public async Task PublishRawAsync(RawEventMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_rawHandlers.TryGetValue(message.EventType, out var rawList))
        {
            List<Func<RawEventMessage, CancellationToken, Task>> copy;
            lock (rawList) { copy = rawList.ToList(); }
            foreach (var handler in copy)
            {
                await handler(message, cancellationToken);
            }
        }

        // Bridge to typed handlers when serializer is available
        foreach (var kvp in _handlers)
        {
            if (!string.Equals(kvp.Key.Name, message.EventType, StringComparison.Ordinal))
            {
                continue;
            }

            if (_serializer is null) continue;
            var payload = _serializer.Deserialize(message.PayloadJson, kvp.Key);
            if (payload is null) continue;

            List<Delegate> copy;
            lock (kvp.Value) { copy = kvp.Value.ToList(); }

            foreach (var del in copy)
            {
                var envelopeType = typeof(EventEnvelope<>).MakeGenericType(kvp.Key);
                var envelope = Activator.CreateInstance(
                    envelopeType,
                    message.EventId,
                    message.CorrelationId,
                    message.CausationId ?? string.Empty,
                    message.TenantId,
                    Guid.Empty,
                    DateTime.UtcNow,
                    1,
                    message.EventType,
                    message.Source,
                    payload,
                    message.Headers is null ? null : new Dictionary<string, string>(message.Headers),
                    null,
                    message.TraceId);

                var result = del.DynamicInvoke(payload, envelope, cancellationToken);
                if (result is Task task) await task;
            }
        }
    }

    public async Task PublishBatchAsync(IReadOnlyList<RawEventMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        foreach (var message in messages)
        {
            await PublishRawAsync(message, cancellationToken);
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
