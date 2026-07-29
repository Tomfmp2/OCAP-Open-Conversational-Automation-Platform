using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;
using OCAP.Tools.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolRegistry _toolRegistry;

    public ToolsController(IToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
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
}
