using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Api.Hubs;
using OCAP.Core.Events;

namespace OCAP.Api.Services;

// Suscriptor que escucha eventos del IEventBus y los retransmite a clientes SignalR respetando el aislamiento Multi-Tenant.
public class LiveGatewayEventSubscriber : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly IHubContext<EventsHub> _hubContext;
    private readonly ILogger<LiveGatewayEventSubscriber>? _logger;

    public LiveGatewayEventSubscriber(
        IEventBus eventBus,
        IHubContext<EventsHub> hubContext,
        ILogger<LiveGatewayEventSubscriber>? logger = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Iniciando LiveGatewayEventSubscriber y suscribiendo a eventos de IEventBus...");

        _eventBus.Subscribe<WorkflowStartedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<WorkflowCompletedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<WorkflowFailedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<NodeExecutedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<AgentStartedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<AgentCompletedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<MessageReceivedEvent>(BroadcastEventAsync);
        _eventBus.Subscribe<MessageSentEvent>(BroadcastEventAsync);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Deteniendo LiveGatewayEventSubscriber...");
        return Task.CompletedTask;
    }

    private async Task BroadcastEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IEvent
    {
        try
        {
            var eventName = @event.GetType().Name;
            var tenantId = GetTenantId(@event);

            if (tenantId == Guid.Empty)
            {
                _logger?.LogWarning("Evento {EventType} recibido sin TenantId válido (ID: {EventId})", eventName, @event.EventId);
                return;
            }

            var groupName = EventsHub.GetTenantGroupName(tenantId);

            await _hubContext.Clients.Group(groupName).SendAsync(eventName, @event, cancellationToken);
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveEvent", eventName, @event, cancellationToken);

            _logger?.LogDebug("Evento {EventType} (ID: {EventId}) transmitido al grupo SignalR {GroupName}",
                eventName, @event.EventId, groupName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retransmitiendo evento SignalR {EventType} (ID: {EventId})",
                @event.GetType().Name, @event.EventId);
        }
    }

    private static Guid GetTenantId<TEvent>(TEvent @event) where TEvent : IEvent
    {
        return @event switch
        {
            WorkflowStartedEvent e => e.TenantId,
            WorkflowCompletedEvent e => e.TenantId,
            WorkflowFailedEvent e => e.TenantId,
            NodeExecutedEvent e => e.TenantId,
            AgentStartedEvent e => e.TenantId,
            AgentCompletedEvent e => e.TenantId,
            MessageReceivedEvent e => e.TenantId,
            MessageSentEvent e => e.TenantId,
            _ => Guid.Empty
        };
    }
}
