using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Security;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Api.Controllers;

// Controlador REST para administración de Roles y Permisos RBAC (CAP-16).
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ISecurityAuditService _auditService;

    public RolesController(OCAPDbContext dbContext, ITenantContext tenantContext, ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == _tenantContext.TenantId)
            .Select(r => new RoleDto(r.Id, r.TenantId, r.Name, r.Description, r.Permissions))
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] RoleDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "El nombre del rol es obligatorio." });

        var existing = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.TenantId == _tenantContext.TenantId && r.Name.ToLower() == request.Name.ToLower().Trim(), cancellationToken);

        if (existing != null)
            return BadRequest(new { error = $"Ya existe un rol con el nombre '{request.Name}'." });

        var role = new Role(Guid.NewGuid(), _tenantContext.TenantId, request.Name.Trim(), request.Description, request.Permissions ?? new List<string>());
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(_tenantContext.TenantId, Guid.Empty, "Role.Created", $"Rol '{role.Name}' creado exitosamente", "RolesController", true, cancellationToken);

        return CreatedAtAction(nameof(GetRoles), new { id = role.Id }, new RoleDto(role.Id, role.TenantId, role.Name, role.Description, role.Permissions));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRole([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.TenantId == _tenantContext.TenantId && r.Id == id, cancellationToken);
        if (role == null) return NotFound(new { error = $"Rol '{id}' no encontrado." });

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(_tenantContext.TenantId, Guid.Empty, "Role.Deleted", $"Rol '{role.Name}' eliminado", "RolesController", true, cancellationToken);

        return Ok(new { message = "Rol eliminado exitosamente." });
    }
}
