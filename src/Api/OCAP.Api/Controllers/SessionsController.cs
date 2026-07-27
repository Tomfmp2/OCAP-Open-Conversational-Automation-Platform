using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<UserSessionDto>> GetSessions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessions = new List<UserSessionDto>
        {
            new(Guid.NewGuid(), userId, tenantId, "192.168.1.50", "Mozilla/5.0 (X11; Linux x86_64) Chrome/126.0.0.0", DateTime.UtcNow.AddMinutes(-45), true),
            new(Guid.NewGuid(), userId, tenantId, "10.0.0.12", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X)", DateTime.UtcNow.AddHours(-5), false)
        };
        return Ok(sessions);
    }
}
