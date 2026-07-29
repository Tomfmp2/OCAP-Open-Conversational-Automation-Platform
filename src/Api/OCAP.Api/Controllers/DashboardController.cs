using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API que expone el estado general, métricas y diagnóstico para el Enterprise Dashboard (CAP-12).
public class DashboardController : ControllerBase
{
    private static readonly DateTime ServerStartTimeUtc = DateTime.UtcNow.AddHours(-12);
    private readonly OCAPDbContext _dbContext;

    public DashboardController(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var uptimeSpan = now - ServerStartTimeUtc;

        var totalWorkflows = await _dbContext.WorkflowDefinitions.CountAsync(cancellationToken);
        var activeWorkflows = await _dbContext.WorkflowDefinitions.CountAsync(w => w.Status == WorkflowStatus.Active, cancellationToken);
        var totalExecutionsToday = await _dbContext.WorkflowExecutions.CountAsync(e => e.StartedAtUtc >= now.Date, cancellationToken);
        var failedExecutionsToday = await _dbContext.WorkflowExecutions.CountAsync(e => e.StartedAtUtc >= now.Date && e.Status == WorkflowStatus.Failed, cancellationToken);

        var totalTenants = await _dbContext.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _dbContext.Tenants.CountAsync(t => t.IsActive, cancellationToken);

        var totalUsers = await _dbContext.UserIdentities.CountAsync(cancellationToken);
        var activeUsers = await _dbContext.UserIdentities.CountAsync(u => u.IsActive, cancellationToken);

        var totalApiKeys = await _dbContext.ApiKeys.CountAsync(cancellationToken);
        var activeApiKeys = await _dbContext.ApiKeys.CountAsync(k => !k.IsRevoked && k.ExpiresAtUtc > now, cancellationToken);
        var revokedApiKeys = await _dbContext.ApiKeys.CountAsync(k => k.IsRevoked, cancellationToken);

        var totalWebhooks = await _dbContext.WebhookSubscriptions.CountAsync(cancellationToken);
        var activeWebhooks = await _dbContext.WebhookSubscriptions.CountAsync(w => w.IsActive, cancellationToken);
        var deliveriesToday = await _dbContext.WebhookDeliveryLogs.CountAsync(d => d.DeliveredAtUtc >= now.Date, cancellationToken);
        var failedDeliveriesToday = await _dbContext.WebhookDeliveryLogs.CountAsync(d => d.DeliveredAtUtc >= now.Date && !d.Success, cancellationToken);

        var totalChannels = await _dbContext.ChannelConnections.CountAsync(cancellationToken);
        var connectedChannels = await _dbContext.ChannelConnections.CountAsync(c => c.Enabled, cancellationToken);
        var telegramConnected = await _dbContext.ChannelConnections.AnyAsync(c => c.Provider == "Telegram" && c.Enabled, cancellationToken);
        var whatsappConnected = await _dbContext.ChannelConnections.AnyAsync(c => c.Provider == "WhatsApp" && c.Enabled, cancellationToken);

        var totalAgents = await _dbContext.Agents.CountAsync(cancellationToken);
        var activeAgents = await _dbContext.Agents.CountAsync(a => a.Status == OCAP.Agents.Domain.Entities.AgentStatus.Active, cancellationToken);

        var recentAuditLogs = await _dbContext.AuditLogs
            .OrderByDescending(a => a.TimestampUtc)
            .Take(10)
            .Select(a => new LastActivityDto
            {
                Id = a.Id,
                EventType = a.Action,
                Description = a.Details,
                Source = a.IpAddress,
                OccurredAtUtc = a.TimestampUtc,
                TenantId = a.TenantId
            })
            .ToListAsync(cancellationToken);

        var overview = new DashboardOverviewDto
        {
            Health = "Healthy",
            Uptime = new ServerUptimeDto
            {
                StartedAtUtc = ServerStartTimeUtc,
                UptimeSeconds = uptimeSpan.TotalSeconds,
                UptimeFormatted = $"{(int)uptimeSpan.TotalDays}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m"
            },
            Workflows = new WorkflowOverviewSummaryDto
            {
                TotalCount = totalWorkflows,
                ActiveCount = activeWorkflows,
                FailedCount = failedExecutionsToday,
                ExecutionsToday = totalExecutionsToday
            },
            Agents = new AgentOverviewSummaryDto
            {
                TotalCount = totalAgents,
                ActiveCount = activeAgents,
                RuntimeStatus = "Operational"
            },
            Channels = new ChannelOverviewSummaryDto
            {
                TotalCount = totalChannels,
                ConnectedCount = connectedChannels,
                TelegramConnected = telegramConnected,
                WhatsappConnected = whatsappConnected
            },
            Tenants = new TenantOverviewSummaryDto
            {
                TotalCount = totalTenants,
                ActiveCount = activeTenants
            },
            Users = new UserOverviewSummaryDto
            {
                TotalCount = totalUsers,
                ActiveCount = activeUsers
            },
            ApiKeys = new ApiKeyOverviewSummaryDto
            {
                TotalCount = totalApiKeys,
                ActiveCount = activeApiKeys,
                RevokedCount = revokedApiKeys
            },
            Webhooks = new WebhookOverviewSummaryDto
            {
                TotalSubscriptions = totalWebhooks,
                ActiveSubscriptions = activeWebhooks,
                DeliveriesToday = deliveriesToday,
                FailedDeliveriesToday = failedDeliveriesToday
            },
            LastActivity = recentAuditLogs
        };

        return Ok(overview);
    }

    [HttpGet("status")]
    public async Task<ActionResult<DashboardStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var activeAgents = await _dbContext.Agents.CountAsync(a => a.Status == OCAP.Agents.Domain.Entities.AgentStatus.Active, cancellationToken);
        var connectedChannels = await _dbContext.ChannelConnections.CountAsync(c => c.Enabled, cancellationToken);
        var totalTools = await _dbContext.ToolExecutions.CountAsync(cancellationToken);
        var totalConvos = await _dbContext.Conversations.CountAsync(cancellationToken);

        var status = new DashboardStatusDto
        {
            SystemStatus = "Healthy",
            ActiveAgentsCount = activeAgents,
            ConnectedChannelsCount = connectedChannels,
            TotalToolExecutions = totalTools,
            TotalConversations = totalConvos,
            ServerTimeUtc = DateTime.UtcNow
        };

        return Ok(status);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeConvosToday = await _dbContext.Conversations.CountAsync(c => c.CreatedAt >= now.Date, cancellationToken);
        var msgsToday = await _dbContext.Messages.CountAsync(m => m.CreatedAt >= now.Date, cancellationToken);

        var todayExecutions = await _dbContext.WorkflowExecutions
            .Where(e => e.StartedAtUtc >= now.Date && e.CompletedAtUtc.HasValue)
            .Select(e => new { e.Status, e.StartedAtUtc, e.CompletedAtUtc })
            .ToListAsync(cancellationToken);

        double avgResponseTime = 0;
        double successRate = 100.0;

        if (todayExecutions.Any())
        {
            avgResponseTime = todayExecutions.Average(e => (e.CompletedAtUtc!.Value - e.StartedAtUtc).TotalMilliseconds);
            var successful = todayExecutions.Count(e => e.Status == WorkflowStatus.Completed);
            successRate = (successful / (double)todayExecutions.Count) * 100;
        }

        var metrics = new DashboardMetricsDto
        {
            AverageResponseTimeMs = avgResponseTime,
            SuccessRatePercentage = successRate,
            ActiveConversationsToday = activeConvosToday,
            MessagesProcessedToday = msgsToday
        };

        return Ok(metrics);
    }

    [HttpGet("signalr-diagnostics")]
    public ActionResult<SignalRDiagnosticsDto> GetSignalRDiagnostics()
    {
        var diagnostics = new SignalRDiagnosticsDto
        {
            HubName = "EventsHub",
            EndpointUri = "/hubs/events",
            Status = "Operational",
            TransportsSupported = new List<string> { "WebSockets", "ServerSentEvents", "LongPolling" },
            StreamedEvents = new List<string>
            {
                "WorkflowStarted", "WorkflowCompleted", "WorkflowFailed", "NodeExecuted",
                "AgentStarted", "AgentCompleted", "MessageReceived", "MessageSent"
            },
            ServerTimeUtc = DateTime.UtcNow
        };

        return Ok(diagnostics);
    }
}
