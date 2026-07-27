using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<UserDto>> GetUsers()
    {
        var tenantId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new(Guid.NewGuid(), tenantId, "admin@ocap.io", "Administrador Principal", true, DateTime.UtcNow.AddDays(-30)),
            new(Guid.NewGuid(), tenantId, "operator@ocap.io", "Operador de Agentes", true, DateTime.UtcNow.AddDays(-10))
        };
        return Ok(users);
    }
}
