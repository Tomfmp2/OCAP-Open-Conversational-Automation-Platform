using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Intelligence.Abstractions;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = OcapPermissions.SettingsManage)]
public class AiProviderConfigurationsController : ControllerBase
{
    private readonly IAiProviderConfigurationService _configurationService;
    private readonly IAiProviderRegistry _providerRegistry;
    private readonly ITenantContext _tenantContext;

    public AiProviderConfigurationsController(
        IAiProviderConfigurationService configurationService,
        IAiProviderRegistry providerRegistry,
        ITenantContext tenantContext)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
    }

    [HttpGet("supported-providers")]
    public ActionResult<IReadOnlyList<string>> GetSupportedProviders()
    {
        return Ok(_providerRegistry.GetRegisteredProviderNames());
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AiProviderConfigurationResponseDto>>> GetTenantConfigurations(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!IsOwnTenant(tenantId)) return NotFound();
        var list = await _configurationService.GetConfigurationsByTenantAsync(tenantId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("tenant/{tenantId:guid}/{id:guid}")]
    public async Task<ActionResult<AiProviderConfigurationResponseDto>> GetConfigurationById(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!IsOwnTenant(tenantId)) return NotFound();
        var config = await _configurationService.GetConfigurationByIdAsync(tenantId, id, cancellationToken);
        if (config == null) return NotFound(new { message = "Configuración no encontrada para el Tenant indicado." });

        return Ok(config);
    }

    [HttpPost]
    public async Task<ActionResult<AiProviderConfigurationResponseDto>> CreateConfiguration(
        [FromBody] CreateAiProviderConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null) return BadRequest(new { message = "Los datos de la configuración son requeridos." });
        // La configuración siempre es de la organización de la sesión, diga lo que diga el cuerpo.
        if (!IsOwnTenant(dto.TenantId)) return NotFound();

        var created = await _configurationService.CreateConfigurationAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetConfigurationById), new { tenantId = created.TenantId, id = created.Id }, created);
    }

    [HttpPut("tenant/{tenantId:guid}/{id:guid}")]
    public async Task<ActionResult<AiProviderConfigurationResponseDto>> UpdateConfiguration(
        Guid tenantId,
        Guid id,
        [FromBody] UpdateAiProviderConfigurationDto dto,
        CancellationToken cancellationToken)
    {
        if (!IsOwnTenant(tenantId)) return NotFound();
        var updated = await _configurationService.UpdateConfigurationAsync(tenantId, id, dto, cancellationToken);
        if (updated == null) return NotFound(new { message = "Configuración no encontrada o no pertenece al Tenant." });

        return Ok(updated);
    }

    [HttpPatch("tenant/{tenantId:guid}/{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid tenantId,
        Guid id,
        [FromQuery] bool enable,
        CancellationToken cancellationToken)
    {
        if (!IsOwnTenant(tenantId)) return NotFound();
        var success = await _configurationService.SetStatusAsync(tenantId, id, enable, cancellationToken);
        if (!success) return NotFound(new { message = "Configuración no encontrada." });

        return Ok(new { message = $"Configuración de proveedor {(enable ? "habilitada" : "deshabilitada")} exitosamente." });
    }

    [HttpDelete("tenant/{tenantId:guid}/{id:guid}")]
    public async Task<IActionResult> DeleteConfiguration(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!IsOwnTenant(tenantId)) return NotFound();
        var success = await _configurationService.DeleteConfigurationAsync(tenantId, id, cancellationToken);
        if (!success) return NotFound(new { message = "Configuración no encontrada." });

        return NoContent();
    }

    [HttpPost("tenant/{tenantId:guid}/test-connection")]
    public async Task<ActionResult<ProviderHealth>> TestTenantProviderConnection(
        Guid tenantId,
        [FromQuery] string? preferredProvider,
        CancellationToken cancellationToken)
    {
        if (!IsOwnTenant(tenantId)) return NotFound();
        var provider = await _configurationService.GetRuntimeProviderForTenantAsync(tenantId, preferredProvider, cancellationToken);
        if (provider is null)
        {
            // Sin proveedor habilitado para el tenant no hay nada que probar (antes era un 500 por NullReferenceException).
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Sin proveedor de IA configurado",
                detail: "Registra y habilita un proveedor en «IA y modelos» antes de probar la conexión.");
        }

        var health = await provider.HealthAsync(cancellationToken);
        return Ok(health);
    }

    /// <summary>El tenant de la URL debe ser el de la sesión (los filtros de EF ya lo garantizan; esto lo hace explícito).</summary>
    private bool IsOwnTenant(Guid tenantId) => tenantId != Guid.Empty && tenantId == _tenantContext.TenantId;
}
