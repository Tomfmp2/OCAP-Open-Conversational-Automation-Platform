using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OCAP.Core.Events.Distributed;
using OCAP.Core.Storage;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

/// <summary>
/// Diagnóstico extendido para el instalador (no sustituye /health/ready|/live).
/// </summary>
[ApiController]
[Route("api/health/diagnostic")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] OCAPDbContext dbContext,
        [FromServices] HealthCheckService healthChecks,
        [FromServices] IEventTransport transport,
        [FromServices] IObjectStorage storage,
        CancellationToken cancellationToken)
    {
        bool dbOk;
        try
        {
            dbOk = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbOk = false;
        }

        var busOk = await transport.HealthCheckAsync(cancellationToken);
        var storageOk = await storage.HealthAsync(cancellationToken);
        var report = await healthChecks.CheckHealthAsync(cancellationToken);

        var steps = new List<object>
        {
            new
            {
                Id = 1,
                Title = "PostgreSQL",
                Description = "Conectividad y disponibilidad de la base de datos relacional/PgVector.",
                Status = dbOk ? "completed" : "error",
                Details = dbOk ? "OK" : "Unreachable"
            },
            new
            {
                Id = 2,
                Title = $"EventBus ({transport.ProviderName})",
                Description = "Disponibilidad del transporte de eventos configurado.",
                Status = busOk ? "completed" : "error",
                Details = busOk ? "OK" : "Unhealthy"
            },
            new
            {
                Id = 3,
                Title = $"Storage ({storage.ProviderName})",
                Description = "Disponibilidad del almacenamiento de objetos configurado.",
                Status = storageOk ? "completed" : "error",
                Details = storageOk ? "OK" : "Unhealthy"
            },
            new
            {
                Id = 4,
                Title = "HealthChecks",
                Description = "Estado agregado de PostgreSQL, Redis, Event Bus, storage y telemetría.",
                Status = report.Status == HealthStatus.Healthy ? "completed" : "error",
                Details = report.Status.ToString()
            }
        };

        var ready = dbOk && report.Status != HealthStatus.Unhealthy;
        return Ok(new
        {
            Status = ready ? "Healthy" : "Degraded",
            IsSystemReady = ready,
            Timestamp = DateTime.UtcNow,
            Steps = steps
        });
    }
}
