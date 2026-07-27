using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API para administrar y consultar los agentes inteligentes registrados.
public class AgentsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<AgentDto>> GetAgents()
    {
        var agents = new List<AgentDto>
        {
            new AgentDto
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Asistente Principal OCAP",
                Description = "Agente para atención general e intenciones del usuario.",
                Status = "Active",
                EnabledTools = new List<string> { "CreateCalendarEventTool", "SendEmailTool" },
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new AgentDto
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Agente de Automatización",
                Description = "Agente para actualización de hojas de cálculo e informes.",
                Status = "Active",
                EnabledTools = new List<string> { "AppendSpreadsheetRowTool" },
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        return Ok(agents);
    }
}
