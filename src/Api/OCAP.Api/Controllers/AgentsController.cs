using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Domain.Entities;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;

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
        var dtos = agents.Select(a => ToDto(a)).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgentDto>> GetAgentById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var agent = await _agentService.GetAgentAsync(id, cancellationToken);
            return Ok(ToDto(agent));
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

        var dto = ToDto(agent);

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

    /// <summary>Herramientas que se le pueden habilitar a un agente de canal (solo las seguras frente a contactos externos).</summary>
    [HttpGet("tools")]
    public ActionResult<IEnumerable<AgentToolDto>> GetTools([FromServices] IToolRegistry tools) =>
        Ok(tools.GetAllTools()
            .Where(t => ChannelAgentRunner.ContactSafeTools.Contains(t.Definition.Name))
            .Select(t => new AgentToolDto(t.Definition.Name, t.Definition.Description)));

    /// <summary>
    /// Prueba un agente desde el panel (id vacío = agente de atención por defecto) con el mismo motor que usan los
    /// canales: prompt, historial y base de conocimiento. No guarda nada ni ejecuta herramientas reales.
    /// </summary>
    [HttpPost("{id:guid}/test")]
    public async Task<ActionResult<AgentTestResponse>> TestAgent(
        Guid id,
        [FromBody] AgentTestRequest request,
        [FromServices] ChannelAgentRunner runner,
        [FromServices] ITenantContext tenantContext,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "El mensaje es obligatorio." });
        if (id != Guid.Empty)
        {
            try { await _agentService.GetAgentAsync(id, cancellationToken); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        var reply = await runner.ReplyAsync(
            new ChannelAgentRequest(
                tenantContext.TenantId,
                id == Guid.Empty ? null : id,
                userContext.UserId,
                Guid.Empty,
                "Prueba del panel",
                "Cliente de prueba",
                (request.History ?? new()).TakeLast(12).Select(t => new ChannelTurn(t.FromContact, t.Text)).ToList(),
                request.Message.Trim(),
                ExecuteTools: false),
            cancellationToken);

        return Ok(new AgentTestResponse(reply.Text, reply.ProviderUsed, reply.AgentName, reply.TransferToHuman));
    }

    private static AgentDto ToDto(Agent agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name.Value,
        Description = agent.Description,
        Status = agent.Status.ToString(),
        SystemPrompt = agent.Configuration.SystemPrompt,
        EnabledTools = agent.Configuration.AllowedToolNames.ToList(),
        CreatedAt = agent.CreatedAt
    };
}

public record AgentTestTurn(bool FromContact, string Text);
public record AgentTestRequest(string Message, List<AgentTestTurn>? History);
public record AgentTestResponse(string Reply, string ProviderUsed, string AgentName, bool TransferToHuman);
public record AgentToolDto(string Name, string Description);

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
