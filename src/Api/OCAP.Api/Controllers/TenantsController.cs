using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Security;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Application.UseCases;

namespace OCAP.Api.Controllers;

// Controlador REST para administración de organizaciones (Tenants) y membresías (CAP-16).
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly CreateTenantUseCase _createTenantUseCase;
    private readonly OCAPDbContext _dbContext;

    public TenantsController(CreateTenantUseCase createTenantUseCase, OCAPDbContext dbContext)
    {
        _createTenantUseCase = createTenantUseCase ?? throw new ArgumentNullException(nameof(createTenantUseCase));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.IsActive, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null) return NotFound(new { error = $"Tenant '{id}' no encontrado." });

        return Ok(new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAtUtc));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var tenant = await _createTenantUseCase.ExecuteAsync(request.Name, request.Slug, Guid.NewGuid(), ip, cancellationToken);
            return Ok(new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAtUtc));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantMembers([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var members = await _dbContext.TenantMembers
            .AsNoTracking()
            .Where(m => m.TenantId == id)
            .Select(m => new { m.Id, m.TenantId, m.UserId, m.RoleId, m.JoinedAtUtc })
            .ToListAsync(cancellationToken);

        return Ok(members);
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddTenantMember([FromRoute] Guid id, [FromBody] AddTenantMemberRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || request.UserId == Guid.Empty)
            return BadRequest(new { error = "El UserId es obligatorio." });

        var existing = await _dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.TenantId == id && m.UserId == request.UserId, cancellationToken);

        if (existing != null)
            return Ok(new { message = "El usuario ya es miembro de esta organización." });

        var roleId = Guid.TryParse(request.Role, out var parsedRoleId) ? parsedRoleId : Guid.NewGuid();
        var member = new OCAP.Security.Domain.Entities.TenantMember(Guid.NewGuid(), id, request.UserId, roleId);
        _dbContext.TenantMembers.Add(member);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Miembro añadido exitosamente a la organización." });
    }
}
