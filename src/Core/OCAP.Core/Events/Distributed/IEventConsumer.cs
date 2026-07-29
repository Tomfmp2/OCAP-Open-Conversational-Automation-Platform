namespace OCAP.Core.Events.Distributed;

// Consumidor y suscriptor para eventos distribuidos (CAP-20).
public interface IEventConsumer
{
    Task SubscribeAsync<TEvent>(Func<TEvent, EventEnvelope<TEvent>, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
