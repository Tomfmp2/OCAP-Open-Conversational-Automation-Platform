using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<RoleDto>> GetRoles()
    {
        var tenantId = Guid.NewGuid();
        var roles = new List<RoleDto>
        {
            new(Guid.NewGuid(), tenantId, "Admin", "Administrador total del tenant", new List<string> { "Conversation.Read", "Conversation.Write", "Conversation.Delete", "Agent.Read", "Agent.Write", "Agent.Execute", "Dashboard.Admin" }),
            new(Guid.NewGuid(), tenantId, "Operator", "Operador conversacional", new List<string> { "Conversation.Read", "Conversation.Write", "Agent.Read", "Agent.Execute" }),
            new(Guid.NewGuid(), tenantId, "Viewer", "Lector de métricas y conversaciones", new List<string> { "Conversation.Read", "Dashboard.Read" })
        };
        return Ok(roles);
    }
}
