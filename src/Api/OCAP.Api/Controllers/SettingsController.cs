using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public class SettingsConfigDto
{
    public string TenantName { get; set; } = "OCAP Enterprise Tenant";
    public string DefaultLocale { get; set; } = "es";
    public string Timezone { get; set; } = "UTC";
    public int AuditLogRetentionDays { get; set; } = 30;
    public bool EnableTelemetry { get; set; } = true;
    public bool EnableFailover { get; set; } = true;
}

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
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
    public async Task<ActionResult<SettingsConfigDto>> GetSettings(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var tenant = tenantId != Guid.Empty
            ? await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            : await _dbContext.Tenants.FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
        {
            return Ok(new SettingsConfigDto());
        }

        SettingsConfigDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<SettingsConfigDto>(tenant.SettingsJson) ?? new SettingsConfigDto();
        }
        catch
        {
            dto = new SettingsConfigDto();
        }

        dto.TenantName = tenant.Name;
        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<SettingsConfigDto>> UpdateSettings([FromBody] SettingsConfigDto request, CancellationToken cancellationToken)
    {
        if (request == null) return BadRequest("Configuración inválida.");

        var tenantId = _tenantContext.TenantId;
        var tenant = tenantId != Guid.Empty
            ? await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            : await _dbContext.Tenants.FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
        {
            return NotFound("Tenant no encontrado para actualizar ajustes.");
        }

        var json = JsonSerializer.Serialize(request);
        tenant.UpdateSettings(json);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogSecurityEventAsync(
            tenant.Id,
            _userContext.UserId,
            "Settings_Updated",
            $"Ajustes globales del tenant {tenant.Name} actualizados.",
            ip,
            true,
            cancellationToken);

        return Ok(request);
    }
}
