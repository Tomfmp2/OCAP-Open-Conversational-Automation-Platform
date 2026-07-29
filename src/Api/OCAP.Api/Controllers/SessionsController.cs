using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Security;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;

    public SessionsController(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<ActionResult<List<UserSessionDto>>> GetSessions(CancellationToken cancellationToken)
    {
        var sessions = await _dbContext.UserSessions
            .Select(s => new UserSessionDto(s.Id, s.UserId, s.TenantId, s.IpAddress, s.UserAgent, s.LoginAtUtc, s.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(sessions);
    }
}
