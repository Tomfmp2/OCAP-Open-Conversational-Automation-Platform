namespace OCAP.Core.Events.Distributed;

// Publicador dedicado para el bus de eventos distribuido (CAP-20).
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
