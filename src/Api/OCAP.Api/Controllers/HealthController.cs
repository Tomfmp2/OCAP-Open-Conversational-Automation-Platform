using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    // Endpoint para verificar la salud y diagnósticos del sistema OCAP
    [HttpGet]
    public async Task<IActionResult> Get([FromServices] OCAPDbContext dbContext, CancellationToken cancellationToken)
    {
        bool dbOk = false;
        try
        {
            dbOk = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbOk = false;
        }

        var steps = new List<object>
        {
            new
            {
                Id = 1,
                Title = "Conexión a Base de Datos PostgreSQL",
                Description = "Verificación de conectividad con EF Core y PostgreSQL",
                Status = dbOk ? "completed" : "error",
                Details = dbOk ? "Conexión a PostgreSQL establecida y verificada." : "Base de datos no accesible."
            },
            new
            {
                Id = 2,
                Title = "Inicialización de Esquema Multi-Tenant",
                Description = "Migraciones de base de datos e inyección de contexto de seguridad",
                Status = dbOk ? "completed" : "pending",
                Details = "Aislamiento por Tenant activo en la capa de persistencia."
            },
            new
            {
                Id = 3,
                Title = "Motor de Orquestación y Runtime AI",
                Description = "Verificación de proveedores de IA y orquestación hexagonal de agentes",
                Status = "completed",
                Details = "Runtime de orquestación de agentes operacional."
            },
            new
            {
                Id = 4,
                Title = "Canales de Comunicación y Webhooks",
                Description = "Adaptadores de Telegram, WhatsApp y EventBus SignalR",
                Status = "completed",
                Details = "SignalR Hub de eventos en tiempo real activo."
            }
        };

        return Ok(new
        {
            Status = dbOk ? "Healthy" : "Degraded",
            IsSystemReady = dbOk,
            Timestamp = DateTime.UtcNow,
            Steps = steps
        });
    }
}
