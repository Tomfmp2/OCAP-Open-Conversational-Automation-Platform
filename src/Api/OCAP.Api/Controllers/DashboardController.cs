using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;
using OCAP.Api.Queries;
using System.Diagnostics;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API que expone el estado general, métricas y diagnóstico para el Enterprise Dashboard (CAP-12).
public class DashboardController : ControllerBase
{
    private readonly DashboardQueries _queries;

    public DashboardController(DashboardQueries queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(CancellationToken cancellationToken) =>
        Ok(await _queries.GetOverviewAsync(cancellationToken));

    [HttpGet("status")]
    public async Task<ActionResult<DashboardStatusDto>> GetStatus(CancellationToken cancellationToken) =>
        Ok(await _queries.GetStatusAsync(cancellationToken));

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics(CancellationToken cancellationToken) =>
        Ok(await _queries.GetMetricsAsync(cancellationToken));

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
    [HttpGet("system-metrics")]
    public ActionResult GetSystemMetrics()
    {
        var process = Process.GetCurrentProcess();
        var uptimeSeconds = (DateTime.UtcNow - DashboardQueries.ServerStartTimeUtc).TotalSeconds;

        // GC proporciona estadísticas de memoria reales del proceso .NET
        var gcMemoryMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 1);
        var processMemoryMb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);

        // CPU: usamos el tiempo de CPU total del proceso para calcular un porcentaje aproximado
        var cpuPercent = Math.Round(
            process.TotalProcessorTime.TotalSeconds / (Environment.ProcessorCount * uptimeSeconds) * 100,
            1);
        cpuPercent = Math.Min(cpuPercent, 100.0); // cap a 100%

        var point = new
        {
            TimeUtc           = DateTime.UtcNow,
            CpuPercent        = cpuPercent,
            MemoryMb          = processMemoryMb,
            GcMemoryMb        = gcMemoryMb,
            UptimeSeconds     = Math.Round(uptimeSeconds, 0),
            ActiveThreads     = process.Threads.Count,
            ProcessorCount    = Environment.ProcessorCount
        };

        return Ok(point);
    }
}
