using OCAP.Core.Events;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato para la gestión de webhooks, despacho de cargas firmadas con HMAC SHA-256 e historial de entrega.
public interface IWebhookService
{
    Task<WebhookSubscription> CreateSubscriptionAsync(
        Guid tenantId,
        string name,
        string targetUrl,
        string secret,
        IEnumerable<string> subscribedEvents,
        CancellationToken cancellationToken = default);

    Task<WebhookSubscription?> UpdateSubscriptionAsync(
        Guid subscriptionId,
        Guid tenantId,
        string name,
        string targetUrl,
        string? secret,
        IEnumerable<string> subscribedEvents,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSubscriptionAsync(Guid subscriptionId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookSubscription>> GetSubscriptionsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookDeliveryLog>> GetDeliveryHistoryAsync(Guid subscriptionId, Guid tenantId, CancellationToken cancellationToken = default);

    Task DispatchEventWebhooksAsync(IEvent @event, CancellationToken cancellationToken = default);
}
