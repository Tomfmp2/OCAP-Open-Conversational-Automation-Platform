namespace OCAP.Core.Events;

// Contrato para los manejadores de eventos.
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
