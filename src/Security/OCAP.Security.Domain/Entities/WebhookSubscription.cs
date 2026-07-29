namespace OCAP.Security.Domain.Entities;

// Entidad que representa la suscripción de un webhook para recibir eventos de OCAP.
public class WebhookSubscription
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty; // Secreto para firma HMAC SHA-256
    public string SubscribedEvents { get; private set; } = string.Empty; // Eventos separados por coma, ej: "WorkflowCompleted,WorkflowFailed,*"
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private WebhookSubscription() { } // ORM

    public WebhookSubscription(Guid id, Guid tenantId, string name, string targetUrl, string secret, string subscribedEvents)
    {
        Id = id;
        TenantId = tenantId;
        Name = name ?? string.Empty;
        TargetUrl = targetUrl ?? string.Empty;
        Secret = secret ?? string.Empty;
        SubscribedEvents = subscribedEvents ?? "*";
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string targetUrl, string secret, string subscribedEvents)
    {
        Name = name;
        TargetUrl = targetUrl;
        if (!string.IsNullOrWhiteSpace(secret)) Secret = secret;
        SubscribedEvents = subscribedEvents;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsSubscribedTo(string eventType)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(eventType)) return false;
        if (string.IsNullOrWhiteSpace(SubscribedEvents) || SubscribedEvents == "*") return true;

        var events = SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (events.Contains("*")) return true;

        var cleanEventType = eventType.EndsWith("Event", StringComparison.OrdinalIgnoreCase)
            ? eventType.Substring(0, eventType.Length - 5)
            : eventType;

        foreach (var ev in events)
        {
            var cleanEv = ev.EndsWith("Event", StringComparison.OrdinalIgnoreCase)
                ? ev.Substring(0, ev.Length - 5)
                : ev;

            if (string.Equals(ev, eventType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleanEv, cleanEventType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
