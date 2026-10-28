using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

/// <summary>Datos de la organización que ve y edita el panel.</summary>
public sealed record OrganizationSettingsDto(string Name);

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private const int MaxNameLength = 256;

    private readonly OCAPDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _auditService;

    public SettingsController(
        OCAPDbContext dbContext,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var tenant = await FindTenantAsync(cancellationToken);
        return tenant is null ? NotFound() : Ok(new OrganizationSettingsDto(tenant.Name));
    }

    [HttpPut]
    [Authorize(Policy = OcapPermissions.SettingsManage)]
    public async Task<ActionResult<OrganizationSettingsDto>> UpdateSettings([FromBody] OrganizationSettingsDto request, CancellationToken cancellationToken)
    {
        var name = request?.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > MaxNameLength)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"El nombre debe tener entre 1 y {MaxNameLength} caracteres.");

        var tenant = await FindTenantAsync(cancellationToken);
        if (tenant is null) return NotFound();

        var previous = tenant.Name;
        tenant.Rename(name);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            tenant.Id,
            _userContext.UserId,
            "Organization_Renamed",
            $"Organización renombrada de '{previous}' a '{tenant.Name}'.",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            true,
            cancellationToken);

        return Ok(new OrganizationSettingsDto(tenant.Name));
    }

    // Solo la organización de la sesión: sin ella no hay datos (antes devolvía la primera de la base).
    private Task<OCAP.Security.Domain.Entities.Tenant?> FindTenantAsync(CancellationToken cancellationToken) =>
        _tenantContext.TenantId == Guid.Empty
            ? Task.FromResult<OCAP.Security.Domain.Entities.Tenant?>(null)
            : _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);
}
