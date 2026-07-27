using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API para consultar el historial de ejecuciones de herramientas.
public class ExecutionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<ExecutionDto>> GetExecutions()
    {
        var executions = new List<ExecutionDto>
        {
            new ExecutionDto
            {
                Id = Guid.NewGuid(),
                AgentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ConversationId = Guid.NewGuid(),
                ToolName = "CreateCalendarEventTool",
                Success = true,
                ExecutedAt = DateTime.UtcNow.AddMinutes(-15)
            },
            new ExecutionDto
            {
                Id = Guid.NewGuid(),
                AgentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ConversationId = Guid.NewGuid(),
                ToolName = "SendEmailTool",
                Success = true,
                ExecutedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        return Ok(executions);
    }
}
