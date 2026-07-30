using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OCAP.Infrastructure.Events.Distributed;

/// <summary>
/// Transporte AMQP real: topic exchange, DLX/DLQ, publisher confirms, prefetch, reconnect.
/// </summary>
public sealed class RabbitMqEventTransport : IEventTransport, IAsyncDisposable
{
    private readonly IEventSerializer _serializer;
    private readonly EventBusOptions _options;
    private readonly ILogger<RabbitMqEventTransport> _logger;
    private readonly ConcurrentDictionary<string, List<RawHandler>> _handlers = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _connected;
    private CancellationTokenSource? _consumerCts;

    public string ProviderName => "RabbitMQ";

    private sealed record RawHandler(Type EventType, Delegate Handler);

    public RabbitMqEventTransport(
        IEventSerializer serializer,
        IOptions<EventBusOptions> options,
        ILogger<RabbitMqEventTransport> logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _options = options?.Value ?? new EventBusOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connected && _connection is { IsOpen: true })
            {
                return;
            }

            await DisposeChannelAsync();

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.ConnectionString),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds)
            };

            _connection = await factory.CreateConnectionAsync("ocap-eventbus", cancellationToken);
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            _channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken);

            await _channel.BasicQosAsync(0, (ushort)_options.PrefetchCount, false, cancellationToken);

            await _channel.ExchangeDeclareAsync(_options.DeadLetterExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(_options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_options.DeadLetterQueue, _options.DeadLetterExchange, routingKey: "#", cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _options.DeadLetterExchange
            };
            await _channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_options.QueueName, _options.ExchangeName, routingKey: "#", cancellationToken: cancellationToken);

            _consumerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await StartConsumerAsync(_consumerCts.Token);

            _connected = true;
            _logger.LogInformation("RabbitMQ transport connected to {Exchange}/{Queue}", _options.ExchangeName, _options.QueueName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _consumerCts?.Cancel();
            await DisposeChannelAsync();
            _connected = false;
            _logger.LogInformation("RabbitMQ transport disconnected.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_connected && _connection is { IsOpen: true } && _channel is { IsOpen: true });

    public async Task PublishAsync<TEvent>(TEvent @event, EventEnvelope<TEvent> envelope, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var raw = new RawEventMessage(
            envelope.EventId,
            envelope.EventType,
            _serializer.Serialize(@event),
            envelope.CorrelationId,
            envelope.TenantId,
            envelope.CausationId,
            envelope.TraceId,
            envelope.Source,
            envelope.Headers);

        await PublishRawAsync(raw, cancellationToken);
    }

    public async Task PublishRawAsync(RawEventMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!_connected || _channel is null || !_channel.IsOpen)
        {
            await ConnectAsync(cancellationToken);
        }

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.EventId,
            CorrelationId = message.CorrelationId,
            Headers = new Dictionary<string, object?>
            {
                ["event-type"] = message.EventType,
                ["tenant-id"] = message.TenantId.ToString("N"),
                ["trace-id"] = message.TraceId ?? string.Empty
            }
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.PublisherConfirmTimeoutMs);

        await _channel!.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: message.EventType,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: timeoutCts.Token);
    }

    public async Task PublishBatchAsync(IReadOnlyList<RawEventMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        foreach (var message in messages)
        {
            await PublishRawAsync(message, cancellationToken);
        }
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        var key = typeof(TEvent).Name;
        var list = _handlers.GetOrAdd(key, _ => new List<RawHandler>());
        lock (list)
        {
            list.Add(new RawHandler(typeof(TEvent), handler));
        }

        return Task.CompletedTask;
    }

    private async Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var raw = JsonSerializer.Deserialize<RawEventMessage>(json)
                          ?? throw new InvalidOperationException("Invalid event payload.");

                if (_handlers.TryGetValue(raw.EventType, out var list))
                {
                    List<RawHandler> copy;
                    lock (list) { copy = list.ToList(); }

                    foreach (var entry in copy)
                    {
                        var payload = _serializer.Deserialize(raw.PayloadJson, entry.EventType)
                                      ?? throw new InvalidOperationException($"Cannot deserialize {raw.EventType}");
                        await InvokeHandlerAsync(entry, payload, raw, cancellationToken);
                    }
                }

                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer failed; sending to retry/DLQ path via nack.");
                await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);
    }

    private static async Task InvokeHandlerAsync(RawHandler entry, object payload, RawEventMessage raw, CancellationToken ct)
    {
        var envelopeType = typeof(EventEnvelope<>).MakeGenericType(entry.EventType);
        var envelope = Activator.CreateInstance(
            envelopeType,
            raw.EventId,
            raw.CorrelationId,
            raw.CausationId ?? string.Empty,
            raw.TenantId,
            Guid.Empty,
            DateTime.UtcNow,
            1,
            raw.EventType,
            raw.Source,
            payload,
            raw.Headers is null ? null : new Dictionary<string, string>(raw.Headers),
            null,
            raw.TraceId);

        var invoke = entry.Handler.Method.Invoke(entry.Handler.Target, new[] { payload, envelope!, ct });
        if (invoke is Task task)
        {
            await task;
        }
    }

    private async Task DisposeChannelAsync()
    {
        if (_channel is not null)
        {
            try { await _channel.CloseAsync(); } catch { /* ignore */ }
            try { await _channel.DisposeAsync(); } catch { /* ignore */ }
            _channel = null;
        }

        if (_connection is not null)
        {
            try { await _connection.CloseAsync(); } catch { /* ignore */ }
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
        _consumerCts?.Dispose();
    }
}
