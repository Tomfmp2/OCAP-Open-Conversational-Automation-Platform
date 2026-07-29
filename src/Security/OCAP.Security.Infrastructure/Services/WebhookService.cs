using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio principal para la gestión de webhooks, despacho de cargas firmadas con HMAC SHA-256 e historial de entrega con persistencia EF Core.
public class WebhookService : IWebhookService
{
    private readonly OCAPDbContext _dbContext;
    private readonly IWebhookSigner _signer;
    private readonly ISecurityAuditService _auditService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService>? _logger;

    public WebhookService(
        OCAPDbContext dbContext,
        IWebhookSigner signer,
        ISecurityAuditService auditService,
        HttpClient? httpClient = null,
        ILogger<WebhookService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<WebhookSubscription> CreateSubscriptionAsync(
        Guid tenantId,
        string name,
        string targetUrl,
        string secret,
        IEnumerable<string> subscribedEvents,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(targetUrl)) throw new ArgumentException("La URL destino es requerida.", nameof(targetUrl));

        var eventsJoined = string.Join(",", subscribedEvents ?? new[] { "*" });
        var sub = new WebhookSubscription(
            Guid.NewGuid(),
            tenantId,
            name,
            targetUrl,
            secret,
            eventsJoined
        );

        _dbContext.WebhookSubscriptions.Add(sub);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Suscripción de Webhook creada (ID: {Id}, Name: {Name}, Target: {TargetUrl}) para Tenant {TenantId}",
            sub.Id, name, targetUrl, tenantId);

        await _auditService.LogSecurityEventAsync(
            tenantId, Guid.Empty, "Webhook.Create", $"Creación de Webhook {name} -> {targetUrl}", "System", true, cancellationToken);

        return sub;
    }

    public async Task<WebhookSubscription?> UpdateSubscriptionAsync(
        Guid subscriptionId,
        Guid tenantId,
        string name,
        string targetUrl,
        string? secret,
        IEnumerable<string> subscribedEvents,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var sub = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId, cancellationToken);
        if (sub == null) return null;

        var eventsJoined = string.Join(",", subscribedEvents ?? new[] { "*" });
        sub.Update(name, targetUrl, secret ?? sub.Secret, eventsJoined);

        if (isActive && !sub.IsActive) sub.Enable();
        else if (!isActive && sub.IsActive) sub.Disable();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Suscripción de Webhook actualizada (ID: {Id}, Target: {TargetUrl}, Active: {IsActive}) para Tenant {TenantId}",
            sub.Id, targetUrl, sub.IsActive, tenantId);

        await _auditService.LogSecurityEventAsync(
            tenantId, Guid.Empty, "Webhook.Update", $"Actualización de Webhook ID {subscriptionId}", "System", true, cancellationToken);

        return sub;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid subscriptionId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sub = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId, cancellationToken);
        if (sub == null) return false;

        _dbContext.WebhookSubscriptions.Remove(sub);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Suscripción de Webhook eliminada (ID: {Id}) para Tenant {TenantId}", subscriptionId, tenantId);

        await _auditService.LogSecurityEventAsync(
            tenantId, Guid.Empty, "Webhook.Delete", $"Eliminación de Webhook ID {subscriptionId}", "System", true, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetSubscriptionsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<WebhookDeliveryLog>> GetDeliveryHistoryAsync(Guid subscriptionId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.WebhookDeliveryLogs
            .Where(l => l.SubscriptionId == subscriptionId && l.TenantId == tenantId)
            .OrderByDescending(l => l.DeliveredAtUtc)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task DispatchEventWebhooksAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType().Name;
        var activeSubs = await _dbContext.WebhookSubscriptions
            .Where(s => s.TenantId == @event.TenantId && s.IsActive)
            .ToListAsync(cancellationToken);

        var matchingSubs = activeSubs.Where(s => s.IsSubscribedTo(eventType)).ToList();
        if (matchingSubs.Count == 0) return;

        var payloadJson = JsonSerializer.Serialize(@event, @event.GetType());

        foreach (var sub in matchingSubs)
        {
            await DeliverPayloadWithRetryAsync(sub, eventType, @event.EventId, payloadJson, cancellationToken);
        }
    }

    private async Task DeliverPayloadWithRetryAsync(
        WebhookSubscription sub,
        string eventType,
        Guid eventId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        int maxAttempts = 3;
        int currentAttempt = 0;
        bool success = false;
        int statusCode = 0;
        string? responseBody = null;
        string? errorMessage = null;

        var signature = _signer.SignPayload(payloadJson, sub.Secret);
        var deliveryId = Guid.NewGuid();

        var stopwatch = Stopwatch.StartNew();

        while (currentAttempt < maxAttempts && !success && !cancellationToken.IsCancellationRequested)
        {
            currentAttempt++;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, sub.TargetUrl);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                request.Headers.Add("X-OCAP-Signature", signature);
                request.Headers.Add("X-OCAP-Event", eventType);
                request.Headers.Add("X-OCAP-Delivery", deliveryId.ToString());
                request.Headers.UserAgent.ParseAdd("OCAP-WebhookDelivery/1.0");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                statusCode = (int)response.StatusCode;
                success = response.IsSuccessStatusCode;

                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (responseBody.Length > 2000) responseBody = responseBody.Substring(0, 2000);

                if (!success)
                {
                    errorMessage = $"HTTP Status {(int)response.StatusCode} {response.ReasonPhrase}";
                    _logger?.LogWarning("Intento {Attempt}/{MaxAttempts} fallido para Webhook {SubId} en {TargetUrl}. HTTP {StatusCode}",
                        currentAttempt, maxAttempts, sub.Id, sub.TargetUrl, statusCode);

                    if (currentAttempt < maxAttempts)
                    {
                        await Task.Delay(100 * currentAttempt, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger?.LogError(ex, "Error en intento {Attempt}/{MaxAttempts} despachando Webhook {SubId} a {TargetUrl}",
                    currentAttempt, maxAttempts, sub.Id, sub.TargetUrl);

                if (currentAttempt < maxAttempts)
                {
                    await Task.Delay(100 * currentAttempt, cancellationToken);
                }
            }
        }

        stopwatch.Stop();

        var log = new WebhookDeliveryLog(
            Guid.NewGuid(),
            sub.Id,
            sub.TenantId,
            eventType,
            eventId,
            sub.TargetUrl,
            statusCode,
            success,
            responseBody,
            errorMessage,
            currentAttempt,
            stopwatch.Elapsed.TotalMilliseconds
        );

        _dbContext.WebhookDeliveryLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!success)
        {
            await _auditService.LogSecurityEventAsync(
                sub.TenantId, Guid.Empty, "Webhook.Delivery.Failed",
                $"Fallo de entrega de Webhook {sub.Id} ({eventType}) a {sub.TargetUrl} tras {currentAttempt} intentos. Error: {errorMessage}",
                "WebhookEngine", false, cancellationToken);
        }
    }
}
