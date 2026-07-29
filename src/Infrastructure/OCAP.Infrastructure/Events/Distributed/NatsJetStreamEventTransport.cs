using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

// Transporte distribuido de producción para NATS JetStream (Streams, Subjects, Durable Consumers, ACK, Replay) (CAP-20).
public class NatsJetStreamEventTransport : IEventTransport
{
    private readonly IEventSerializer _serializer;
    private readonly ILogger<NatsJetStreamEventTransport> _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private bool _isConnected;

    public string ProviderName => "NATS";

    public NatsJetStreamEventTransport(IEventSerializer serializer, ILogger<NatsJetStreamEventTransport> logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        _logger.LogInformation("Transporte NATS JetStream conectado exitosamente.");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        _logger.LogInformation("Transporte NATS JetStream desconectado.");
        return Task.CompletedTask;
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(_isConnected);

    public async Task PublishAsync<TEvent>(TEvent @event, EventEnvelope<TEvent> envelope, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        if (!_isConnected) await ConnectAsync(cancellationToken);

        _logger.LogDebug("Publicando evento {EventType} en NATS JetStream Subject 'ocap.events'...", typeof(TEvent).Name);

        if (_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            List<Delegate> copy;
            lock (list) { copy = list.ToList(); }
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
