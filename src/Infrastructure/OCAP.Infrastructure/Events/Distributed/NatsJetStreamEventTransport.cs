using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using OCAP.Core.Events;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

/// <summary>
/// Transporte NATS JetStream real: stream durable, ack/nak, competing consumers, reconnect.
/// </summary>
public sealed class NatsJetStreamEventTransport : IEventTransport, IAsyncDisposable
{
    private readonly IEventSerializer _serializer;
    private readonly EventBusOptions _options;
    private readonly ILogger<NatsJetStreamEventTransport> _logger;
    private readonly ConcurrentDictionary<string, List<RawHandler>> _handlers = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);

    private NatsConnection? _connection;
    private INatsJSContext? _js;
    private CancellationTokenSource? _consumerCts;
    private bool _connected;

    public string ProviderName => "NATS";

    private sealed record RawHandler(Type EventType, Delegate Handler);

    public NatsJetStreamEventTransport(
        IEventSerializer serializer,
        IOptions<EventBusOptions> options,
        ILogger<NatsJetStreamEventTransport> logger)
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
            if (_connected && _connection is not null)
            {
                return;
            }

            await DisposeConnectionAsync();

            var opts = new NatsOpts
            {
                Url = _options.NatsUrl,
                Name = "ocap-eventbus",
                MaxReconnectRetry = -1,
                ReconnectWaitMin = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds)
            };

            _connection = new NatsConnection(opts);
            await _connection.ConnectAsync();
            _js = new NatsJSContext(_connection);

            await EnsureStreamAsync(cancellationToken);

            _consumerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(() => ConsumeLoopAsync(_consumerCts.Token), CancellationToken.None);

            _connected = true;
            _logger.LogInformation("NATS JetStream connected to {Url} stream {Stream}", _options.NatsUrl, _options.JetStreamName);
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
            await DisposeConnectionAsync();
            _connected = false;
            _logger.LogInformation("NATS JetStream disconnected.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_connected && _connection is not null && _connection.ConnectionState == NatsConnectionState.Open);

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
        if (!_connected || _js is null)
        {
            await ConnectAsync(cancellationToken);
        }

        var subject = $"ocap.events.{message.EventType}";
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var ack = await _js!.PublishAsync(subject, payload, cancellationToken: cancellationToken);
        ack.EnsureSuccess();
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

    private async Task EnsureStreamAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _js!.CreateStreamAsync(new StreamConfig(_options.JetStreamName, new[] { "ocap.events.>" })
            {
                Storage = StreamConfigStorage.File,
                Retention = StreamConfigRetention.Limits,
                MaxMsgs = -1,
                DuplicateWindow = TimeSpan.FromMinutes(2)
            }, cancellationToken);
        }
        catch (NatsJSApiException)
        {
            // Stream may already exist.
        }
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_js is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), cancellationToken);
                    continue;
                }

                var consumer = await _js.CreateOrUpdateConsumerAsync(_options.JetStreamName, new ConsumerConfig(_options.ConsumerGroup)
                {
                    DurableName = _options.ConsumerGroup,
                    AckPolicy = ConsumerConfigAckPolicy.Explicit,
                    MaxDeliver = _options.MaxRetries,
                    FilterSubject = "ocap.events.>"
                }, cancellationToken);

                await foreach (var msg in consumer.ConsumeAsync<byte[]>(opts: new NatsJSConsumeOpts { MaxMsgs = _options.PrefetchCount }, cancellationToken: cancellationToken))
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(msg.Data ?? Array.Empty<byte>());
                        var raw = JsonSerializer.Deserialize<RawEventMessage>(json);
                        if (raw is null)
                        {
                            await msg.NakAsync(cancellationToken: cancellationToken);
                            continue;
                        }

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

                        await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "NATS consumer handler failed");
                        await msg.NakAsync(cancellationToken: cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NATS consume loop interrupted; reconnecting");
                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), cancellationToken);
            }
        }
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

    private async Task DisposeConnectionAsync()
    {
        if (_connection is not null)
        {
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
            _connection = null;
            _js = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
        _consumerCts?.Dispose();
    }
}
