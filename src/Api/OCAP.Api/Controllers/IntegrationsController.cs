using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;

    public IntegrationsController(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("google")]
    public async Task<ActionResult<GoogleIntegrationDto>> GetGoogleIntegration(CancellationToken cancellationToken)
    {
        var conn = await _dbContext.OAuthConnections.FirstOrDefaultAsync(c => c.Provider == "Google", cancellationToken);
        var integration = new GoogleIntegrationDto
        {
            IsConnected = conn != null,
            AccountEmail = conn != null ? "connected@google" : "not-connected",
            OAuthStatus = conn != null ? "Authorized" : "Not Authorized",
            GrantedScopes = conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
            LastSyncedAt = conn?.UpdatedAt ?? DateTime.UtcNow
        };

        return Ok(integration);
    }
}
