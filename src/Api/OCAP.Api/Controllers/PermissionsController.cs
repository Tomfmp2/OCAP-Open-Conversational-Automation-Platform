using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Security;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;

    public PermissionsController(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Category, p.Description))
            .ToListAsync(cancellationToken);
        return Ok(permissions);
    }
}
