namespace OCAP.Core.Events;

// Contrato agnóstico del Bus de Eventos en tiempo real para publicar y suscribir eventos.
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent;
    void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
}
