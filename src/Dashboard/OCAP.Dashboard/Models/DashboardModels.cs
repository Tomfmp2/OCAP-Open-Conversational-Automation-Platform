namespace OCAP.Dashboard.Models;

public class DashboardStatusModel
{
    public string SystemStatus { get; set; } = "Healthy";
    public int ActiveAgentsCount { get; set; }
    public int ConnectedChannelsCount { get; set; }
    public long TotalToolExecutions { get; set; }
    public long TotalConversations { get; set; }
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}

public class DashboardMetricsModel
{
    public double AverageResponseTimeMs { get; set; }
    public double SuccessRatePercentage { get; set; }
    public int ActiveConversationsToday { get; set; }
    public int MessagesProcessedToday { get; set; }
}

public class AgentModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public List<string> EnabledTools { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ToolModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "Active";
    public List<string> RequiredPermissions { get; set; } = new();
}

public class ExecutionModel
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid ConversationId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class ConversationModel
{
    public Guid Id { get; set; }
    public string Channel { get; set; } = "WhatsApp";
    public string UserIdentifier { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
}

public class GoogleIntegrationModel
{
    public bool IsConnected { get; set; } = true;
    public string AccountEmail { get; set; } = "workspace-admin@ocap.org";
    public string OAuthStatus { get; set; } = "Authorized";
    public List<string> GrantedScopes { get; set; } = new();
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
