using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API para administrar y consultar los agentes inteligentes registrados (CAP-12).
public class AgentsController : ControllerBase
{
    private static readonly List<AgentDto> SeedAgents = new()
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

    [HttpGet]
    public ActionResult<IEnumerable<AgentDto>> GetAgents()
    {
        return Ok(SeedAgents);
    }

    [HttpGet("{id}")]
    public ActionResult<AgentDto> GetAgentById(Guid id)
    {
        var agent = SeedAgents.FirstOrDefault(a => a.Id == id);
        if (agent == null)
        {
            var fallback = new AgentDto
            {
                Id = id,
                Name = "Agente Dinámico OCAP",
                Description = "Agente para tareas configurables del usuario.",
                Status = "Active",
                EnabledTools = new List<string> { "GenericTool" },
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            return Ok(fallback);
        }

        return Ok(agent);
    }

    [HttpGet("{id}/status")]
    public ActionResult<AgentRuntimeStatusDto> GetAgentRuntimeStatus(Guid id)
    {
        var status = new AgentRuntimeStatusDto
        {
            AgentId = id,
            Status = "Operational",
            ActiveConversationsCount = 3,
            MessagesProcessedTotal = 156,
            AverageResponseTimeMs = 320.5,
            LastExecutedAtUtc = DateTime.UtcNow.AddMinutes(-2)
        };

        return Ok(status);
    }

    [HttpGet("status")]
    public ActionResult<List<AgentRuntimeStatusDto>> GetAllAgentsRuntimeStatus()
    {
        var statuses = SeedAgents.Select(a => new AgentRuntimeStatusDto
        {
            AgentId = a.Id,
            Status = "Operational",
            ActiveConversationsCount = 2,
            MessagesProcessedTotal = 98,
            AverageResponseTimeMs = 285.0,
            LastExecutedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        }).ToList();

        return Ok(statuses);
    }
}

public class AgentRuntimeStatusDto
{
    public Guid AgentId { get; set; }
    public string Status { get; set; } = "Operational";
    public int ActiveConversationsCount { get; set; }
    public long MessagesProcessedTotal { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public DateTime LastExecutedAtUtc { get; set; }
}
