namespace OCAP.Security.Domain.Entities;

// Registra la historia de envíos de webhooks y reintentos.
public class WebhookDeliveryLog
{
    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid EventId { get; private set; }
    public string TargetUrl { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public bool Success { get; private set; }
    public string? ResponseBody { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AttemptCount { get; private set; }
    public double DurationMs { get; private set; }
    public DateTime DeliveredAtUtc { get; private set; }

    private WebhookDeliveryLog() { } // ORM

    public WebhookDeliveryLog(
        Guid id,
        Guid subscriptionId,
        Guid tenantId,
        string eventType,
        Guid eventId,
        string targetUrl,
        int statusCode,
        bool success,
        string? responseBody,
        string? errorMessage,
        int attemptCount,
        double durationMs)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        EventType = eventType;
        EventId = eventId;
        TargetUrl = targetUrl;
        StatusCode = statusCode;
        Success = success;
        ResponseBody = responseBody;
        ErrorMessage = errorMessage;
        AttemptCount = attemptCount;
        DurationMs = durationMs;
        DeliveredAtUtc = DateTime.UtcNow;
    }
}
