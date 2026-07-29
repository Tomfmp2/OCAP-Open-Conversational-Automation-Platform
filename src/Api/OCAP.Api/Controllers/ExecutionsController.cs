using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExecutionsController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;

    public ExecutionsController(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExecutionDto>>> GetExecutions(CancellationToken cancellationToken)
    {
        var executions = await _dbContext.ToolExecutions
            .OrderByDescending(e => e.ExecutedAt)
            .Take(50)
            .Select(e => new ExecutionDto
            {
                Id = e.Id,
                AgentId = e.AgentId,
                ConversationId = e.ConversationId,
                ToolName = e.ToolName,
                Success = e.Success,
                ExecutedAt = e.ExecutedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(executions);
    }
}
