namespace OCAP.Api.Models.Dashboard;

// DTO estandarizado para resumir el estado operacional del sistema OCAP en el Dashboard.
public class DashboardStatusDto
{
    public string SystemStatus { get; set; } = "Healthy";
    public int ActiveAgentsCount { get; set; }
    public int ConnectedChannelsCount { get; set; }
    public long TotalToolExecutions { get; set; }
    public long TotalConversations { get; set; }
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}
