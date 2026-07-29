namespace OCAP.Core.Events.Distributed;

// Contrato agnóstico para transportes del bus de eventos (RabbitMQ, NATS, InMemory, Azure SB, AWS SQS) (CAP-20).
public interface IEventTransport
{
    string ProviderName { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task PublishAsync<TEvent>(TEvent @event, EventEnvelope<TEvent> envelope, CancellationToken cancellationToken = default) where TEvent : IEvent;
    Task SubscribeAsync<TEvent>(Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IEvent;
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
