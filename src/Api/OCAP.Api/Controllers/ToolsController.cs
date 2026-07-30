using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;
using OCAP.Api.Security;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;

namespace OCAP.Api.Controllers;

public class ExecuteToolRequestDto
{
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Guid? AgentId { get; set; }
    public Guid? ConversationId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IUserContext _userContext;

    public ToolsController(IToolRegistry toolRegistry, IUserContext userContext)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpGet]
    public ActionResult<IEnumerable<ToolDto>> GetTools()
    {
        var tools = _toolRegistry.GetAllTools().Select(t => new ToolDto
        {
            Id = t.Definition.Id,
            Name = t.Definition.Name,
            Description = t.Definition.Description,
            Version = t.Definition.Version,
            Status = t.Definition.Status.ToString(),
            RequiredPermissions = t.Definition.RequiredPermissions?.ToList() ?? new List<string>()
        }).ToList();

        return Ok(tools);
    }

    [HttpGet("{id}")]
    public ActionResult<ToolDto> GetToolById(string id)
    {
        var tool = _toolRegistry.GetTool(id);
        if (tool == null) return NotFound($"Herramienta '{id}' no encontrada en el registro activo.");

        var dto = new ToolDto
        {
            Id = tool.Definition.Id,
            Name = tool.Definition.Name,
            Description = tool.Definition.Description,
            Version = tool.Definition.Version,
            Status = tool.Definition.Status.ToString(),
            RequiredPermissions = tool.Definition.RequiredPermissions?.ToList() ?? new List<string>()
        };

        return Ok(dto);
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<ToolResult>> ExecuteTool(string id, [FromBody] ExecuteToolRequestDto request, CancellationToken cancellationToken)
    {
        var tool = _toolRegistry.GetTool(id);
        if (tool == null) return NotFound($"Herramienta '{id}' no encontrada.");

        var userId = TenantSecurity.RequireUserId(_userContext);
        var context = new ToolExecutionContext(
            agentId: request.AgentId ?? Guid.Empty,
            userId: userId,
            conversationId: request.ConversationId ?? Guid.Empty,
            parameters: request.Parameters ?? new()
        );

        var result = await tool.ExecuteAsync(context, cancellationToken);
        return Ok(result);
    }
}
