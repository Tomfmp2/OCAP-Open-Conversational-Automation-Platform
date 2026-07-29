using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST de administración de sincronización de directorios (LDAP / Active Directory / SCIM) (CAP-19).
[ApiController]
[Route("api/directory")]
[Authorize]
public class DirectorySyncController : ControllerBase
{
    private readonly ILdapService _ldapService;
    private readonly IDirectorySyncEngine _syncEngine;
    private readonly ITenantContext _tenantContext;

    public DirectorySyncController(ILdapService ldapService, IDirectorySyncEngine syncEngine, ITenantContext tenantContext)
    {
        _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
        _syncEngine = syncEngine ?? throw new ArgumentNullException(nameof(syncEngine));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet("ldap/config")]
    [ProducesResponseType(typeof(LdapConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLdapConfig(CancellationToken cancellationToken)
    {
        var config = await _ldapService.GetLdapConfigAsync(_tenantContext.TenantId, cancellationToken);
        if (config == null) return NotFound(new { error = "Configuración LDAP no encontrada para el tenant." });

        return Ok(config);
    }

    [HttpPost("ldap/config")]
    [ProducesResponseType(typeof(LdapConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveLdapConfig([FromBody] SaveLdapConfigDto dto, CancellationToken cancellationToken)
    {
        var config = await _ldapService.SaveLdapConfigAsync(_tenantContext.TenantId, dto, cancellationToken);
        return Ok(config);
    }

    [HttpPost("ldap/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestLdapConnection([FromBody] SaveLdapConfigDto dto, CancellationToken cancellationToken)
    {
        var success = await _ldapService.TestConnectionAsync(_tenantContext.TenantId, dto, cancellationToken);
        return Ok(new { success, message = success ? "Conexión a servidor LDAP exitosa." : "Fallo en la prueba de conexión LDAP." });
    }

    [HttpPost("sync/trigger")]
    [ProducesResponseType(typeof(SyncStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerSync([FromQuery] string providerType = "LDAP", [FromQuery] string syncType = "Full", CancellationToken cancellationToken = default)
    {
        var status = await _syncEngine.TriggerSyncJobAsync(_tenantContext.TenantId, providerType, syncType, cancellationToken);
        return Ok(status);
    }

    [HttpGet("sync/status")]
    [ProducesResponseType(typeof(SyncStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSyncStatus(CancellationToken cancellationToken)
    {
        var status = await _syncEngine.GetSyncStatusAsync(_tenantContext.TenantId, cancellationToken);
        if (status == null) return NotFound(new { error = "No hay trabajos de sincronización registrados para el tenant." });

        return Ok(status);
    }

    [HttpGet("sync/history")]
    [ProducesResponseType(typeof(List<SyncHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncHistory([FromQuery] int top = 50, CancellationToken cancellationToken = default)
    {
        var history = await _syncEngine.GetSyncHistoryAsync(_tenantContext.TenantId, top, cancellationToken);
        return Ok(history);
    }
}
