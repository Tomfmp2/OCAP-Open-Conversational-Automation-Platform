namespace OCAP.Api.Models.Dashboard;

// DTO integral para la visión general del Enterprise Dashboard en CAP-12
public class DashboardOverviewDto
{
    public string Health { get; set; } = "Healthy";
    public ServerUptimeDto Uptime { get; set; } = new();
    public WorkflowOverviewSummaryDto Workflows { get; set; } = new();
    public AgentOverviewSummaryDto Agents { get; set; } = new();
    public ChannelOverviewSummaryDto Channels { get; set; } = new();
    public TenantOverviewSummaryDto Tenants { get; set; } = new();
    public UserOverviewSummaryDto Users { get; set; } = new();
    public ApiKeyOverviewSummaryDto ApiKeys { get; set; } = new();
    public WebhookOverviewSummaryDto Webhooks { get; set; } = new();
    public List<LastActivityDto> LastActivity { get; set; } = new();
}

public class ServerUptimeDto
{
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow.AddDays(-3);
    public string UptimeFormatted { get; set; } = "3 días, 4 horas, 12 minutos";
    public double UptimeSeconds { get; set; } = 274320;
}

public class WorkflowOverviewSummaryDto
{
    public int TotalCount { get; set; } = 5;
    public int ActiveCount { get; set; } = 4;
    public int FailedCount { get; set; } = 0;
    public int ExecutionsToday { get; set; } = 128;
}

public class AgentOverviewSummaryDto
{
    public int TotalCount { get; set; } = 3;
    public int ActiveCount { get; set; } = 3;
    public string RuntimeStatus { get; set; } = "Operational";
}

public class ChannelOverviewSummaryDto
{
    public int TotalCount { get; set; } = 2;
    public int ConnectedCount { get; set; } = 2;
    public bool TelegramConnected { get; set; } = true;
    public bool WhatsappConnected { get; set; } = true;
}

public class TenantOverviewSummaryDto
{
    public int TotalCount { get; set; } = 1;
    public int ActiveCount { get; set; } = 1;
}

public class UserOverviewSummaryDto
{
    public int TotalCount { get; set; } = 4;
    public int ActiveCount { get; set; } = 4;
}

public class ApiKeyOverviewSummaryDto
{
    public int TotalCount { get; set; } = 3;
    public int ActiveCount { get; set; } = 3;
    public int RevokedCount { get; set; } = 0;
}

public class WebhookOverviewSummaryDto
{
    public int TotalSubscriptions { get; set; } = 2;
    public int ActiveSubscriptions { get; set; } = 2;
    public int DeliveriesToday { get; set; } = 45;
    public int FailedDeliveriesToday { get; set; } = 0;
}

public class LastActivityDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

public class SignalRDiagnosticsDto
{
    public string HubName { get; set; } = "EventsHub";
    public string EndpointUri { get; set; } = "/hubs/events";
    public string Status { get; set; } = "Operational";
    public List<string> TransportsSupported { get; set; } = new() { "WebSockets", "ServerSentEvents", "LongPolling" };
    public List<string> StreamedEvents { get; set; } = new()
    {
        "WorkflowStarted", "WorkflowCompleted", "WorkflowFailed", "NodeExecuted",
        "AgentStarted", "AgentCompleted", "MessageReceived", "MessageSent"
    };
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}
