using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API que expone el estado general y métricas del sistema para el Dashboard.
public class DashboardController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<DashboardStatusDto> GetStatus()
    {
        var status = new DashboardStatusDto
        {
            SystemStatus = "Healthy",
            ActiveAgentsCount = 2,
            ConnectedChannelsCount = 1,
            TotalToolExecutions = 28,
            TotalConversations = 14,
            ServerTimeUtc = DateTime.UtcNow
        };

        return Ok(status);
    }

    [HttpGet("metrics")]
    public ActionResult<DashboardMetricsDto> GetMetrics()
    {
        var metrics = new DashboardMetricsDto
        {
            AverageResponseTimeMs = 38.2,
            SuccessRatePercentage = 99.9,
            ActiveConversationsToday = 14,
            MessagesProcessedToday = 210
        };

        return Ok(metrics);
    }
}
