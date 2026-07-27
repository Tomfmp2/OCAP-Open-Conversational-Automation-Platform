namespace OCAP.Api.Models.Dashboard;

// DTO de métricas de telemetría y rendimiento para el panel de administración.
public class DashboardMetricsDto
{
    public double AverageResponseTimeMs { get; set; } = 42.5;
    public double SuccessRatePercentage { get; set; } = 99.8;
    public int ActiveConversationsToday { get; set; } = 15;
    public int MessagesProcessedToday { get; set; } = 340;
}
