using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

// Suscriptor en tiempo real que escucha todos los eventos del IEventBus y los canaliza hacia el motor de despacho de Webhooks.
public class WebhookEventSubscriber : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookEventSubscriber>? _logger;

    public WebhookEventSubscriber(
        IEventBus eventBus,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookEventSubscriber>? logger = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Iniciando WebhookEventSubscriber y suscribiendo a IEventBus...");

        _eventBus.Subscribe<WorkflowStartedEvent>(OnEventAsync);
        _eventBus.Subscribe<WorkflowCompletedEvent>(OnEventAsync);
        _eventBus.Subscribe<WorkflowFailedEvent>(OnEventAsync);
        _eventBus.Subscribe<NodeExecutedEvent>(OnEventAsync);
        _eventBus.Subscribe<AgentStartedEvent>(OnEventAsync);
        _eventBus.Subscribe<AgentCompletedEvent>(OnEventAsync);
        _eventBus.Subscribe<MessageReceivedEvent>(OnEventAsync);
        _eventBus.Subscribe<MessageSentEvent>(OnEventAsync);
        _eventBus.Subscribe<ConversationStartedEvent>(OnEventAsync);
        _eventBus.Subscribe<ConversationClosedEvent>(OnEventAsync);
        _eventBus.Subscribe<HumanInterventionRequestedEvent>(OnEventAsync);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Deteniendo WebhookEventSubscriber...");
        return Task.CompletedTask;
    }

    private async Task OnEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IEvent
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
            await webhookService.DispatchEventWebhooksAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error no controlado despachando Webhooks para el evento {EventType} (ID: {EventId})",
                @event.GetType().Name, @event.EventId);
        }
    }
}
