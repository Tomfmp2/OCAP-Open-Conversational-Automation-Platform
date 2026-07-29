using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Domain.Entities;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly AgentService _agentService;

    public AgentsController(AgentService agentService)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgents(CancellationToken cancellationToken)
    {
        var agents = await _agentService.GetAllAgentsAsync(cancellationToken);
        var dtos = agents.Select(a => new AgentDto
        {
            Id = a.Id,
            Name = a.Name.Value,
            Description = a.Description,
            Status = a.Status.ToString(),
            EnabledTools = a.Configuration.AllowedToolNames.ToList(),
            CreatedAt = a.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgentDto>> GetAgentById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var agent = await _agentService.GetAgentAsync(id, cancellationToken);
            return Ok(new AgentDto
            {
                Id = agent.Id,
                Name = agent.Name.Value,
                Description = agent.Description,
                Status = agent.Status.ToString(),
                EnabledTools = agent.Configuration.AllowedToolNames.ToList(),
                CreatedAt = agent.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<AgentDto>> CreateAgent([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var agent = await _agentService.CreateAgentAsync(
            request.Name, 
            request.Description, 
            request.SystemPrompt, 
            request.AllowedTools ?? new List<string>(), 
            cancellationToken);

        var dto = new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name.Value,
            Description = agent.Description,
            Status = agent.Status.ToString(),
            EnabledTools = agent.Configuration.AllowedToolNames.ToList(),
            CreatedAt = agent.CreatedAt
        };

        return CreatedAtAction(nameof(GetAgentById), new { id = agent.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAgent(Guid id, [FromBody] UpdateAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _agentService.UpdateAgentAsync(id, request.Name, request.Description, request.SystemPrompt, request.AllowedTools ?? new List<string>(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAgent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _agentService.DeleteAgentAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<AgentRuntimeStatusDto>> GetAgentRuntimeStatus(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var agent = await _agentService.GetAgentAsync(id, cancellationToken);
            
            // Currently returns the operational status based on the real agent.
            // Runtime stats are not currently tracked at the agent level in this version.
            var status = new AgentRuntimeStatusDto
            {
                AgentId = id,
                Status = agent.Status.ToString(),
                ActiveConversationsCount = 0,
                MessagesProcessedTotal = 0,
                AverageResponseTimeMs = 0,
                LastExecutedAtUtc = agent.UpdatedAt ?? agent.CreatedAt
            };

            return Ok(status);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<List<AgentRuntimeStatusDto>>> GetAllAgentsRuntimeStatus(CancellationToken cancellationToken)
    {
        var agents = await _agentService.GetAllAgentsAsync(cancellationToken);
        var statuses = agents.Select(agent => new AgentRuntimeStatusDto
        {
            AgentId = agent.Id,
            Status = agent.Status.ToString(),
            ActiveConversationsCount = 0,
            MessagesProcessedTotal = 0,
            AverageResponseTimeMs = 0,
            LastExecutedAtUtc = agent.UpdatedAt ?? agent.CreatedAt
        }).ToList();

        return Ok(statuses);
    }
}

public record CreateAgentRequest(string Name, string Description, string SystemPrompt, List<string> AllowedTools);
public record UpdateAgentRequest(string Name, string Description, string SystemPrompt, List<string> AllowedTools);

public class AgentRuntimeStatusDto
{
    public Guid AgentId { get; set; }
    public string Status { get; set; } = "Operational";
    public int ActiveConversationsCount { get; set; }
    public long MessagesProcessedTotal { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public DateTime LastExecutedAtUtc { get; set; }
}
