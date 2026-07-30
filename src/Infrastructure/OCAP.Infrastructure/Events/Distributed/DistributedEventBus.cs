using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

/// <summary>
/// Bus distribuido con outbox, inbox idempotente, DLQ y despacho inmediato opcional.
/// </summary>
public class DistributedEventBus : IEventBus
{
    private readonly IEventTransport _transport;
    private readonly IEventSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DistributedEventBus> _logger;
    private readonly EventBusOptions _options;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlerDelegates = new();

    public DistributedEventBus(
        IEventTransport transport,
        IEventSerializer serializer,
        IServiceProvider serviceProvider,
        ILogger<DistributedEventBus> logger,
        IOptions<EventBusOptions>? options = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new EventBusOptions();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent).Name;
        var payloadJson = _serializer.Serialize(@event);
        var eventId = Guid.NewGuid();
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

        var envelope = new EventEnvelope<TEvent>(
            EventId: eventId.ToString("N"),
            CorrelationId: correlationId,
            CausationId: Guid.NewGuid().ToString("N"),
            TenantId: @event.TenantId,
            UserId: Guid.Empty,
            Timestamp: DateTime.UtcNow,
            Version: 1,
            EventType: eventType,
            Source: Environment.MachineName,
            Payload: @event,
            Headers: new Dictionary<string, string>
            {
                ["tenant-id"] = @event.TenantId.ToString("N"),
                ["trace-id"] = Activity.Current?.TraceId.ToString() ?? string.Empty
            },
            TraceId: Activity.Current?.TraceId.ToString());

        if (_options.EnableOutbox)
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxStore = scope.ServiceProvider.GetService<IOutboxStore>();
            if (outboxStore != null)
            {
                var outboxMsg = new OutboxMessage(eventId, @event.TenantId, eventType, payloadJson);
                await outboxStore.SaveAsync(outboxMsg, cancellationToken);

                if (_options.ImmediateDispatch)
                {
                    await _transport.PublishAsync(@event, envelope, cancellationToken);
                    await outboxStore.MarkAsProcessedAsync(eventId, cancellationToken);
                }
            }
            else if (_options.ImmediateDispatch)
            {
                await _transport.PublishAsync(@event, envelope, cancellationToken);
            }
        }
        else
        {
            await _transport.PublishAsync(@event, envelope, cancellationToken);
        }

        _logger.LogInformation(
            "Evento {EventType} registrado (provider={Provider}, immediate={Immediate})",
            eventType, _transport.ProviderName, _options.ImmediateDispatch);
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        Subscribe<TEvent>((@event, ct) => handler.HandleAsync(@event, ct));
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> envelopeHandler = async (@event, envelope, ct) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var inbox = _options.EnableInbox
                ? scope.ServiceProvider.GetService<IInboxStore>()
                : null;
            var dlq = scope.ServiceProvider.GetService<IMessageDeadLetterHandler>();
            var retry = scope.ServiceProvider.GetService<IMessageRetryPolicy>();

            if (inbox != null && await inbox.HasBeenProcessedAsync(envelope.EventId, _options.ConsumerGroup, ct))
            {
                _logger.LogDebug("Inbox skip duplicate {EventId}", envelope.EventId);
                return;
            }

            try
            {
                if (retry != null)
                {
                    await retry.ExecuteWithRetryAsync(() => handler(@event, ct), ct);
                }
                else
                {
                    await handler(@event, ct);
                }

                if (inbox != null)
                {
                    await inbox.MarkAsProcessedAsync(envelope.EventId, _options.ConsumerGroup, ct);
                }
            }
            catch (Exception ex)
            {
                if (dlq != null)
                {
                    await dlq.HandleDeadLetterAsync(
                        envelope.TenantId,
                        envelope.EventType,
                        _serializer.Serialize(@event),
                        ex.Message,
                        retry?.MaxRetries ?? _options.MaxRetries,
                        ct);
                }

                throw;
            }
        };

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
