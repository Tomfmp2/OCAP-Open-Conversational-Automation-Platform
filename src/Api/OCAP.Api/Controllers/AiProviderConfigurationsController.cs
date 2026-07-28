using Microsoft.AspNetCore.Mvc;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiProviderConfigurationsController : ControllerBase
{
    private readonly IAiProviderConfigurationService _configurationService;
    private readonly IAiProviderRegistry _providerRegistry;

    public AiProviderConfigurationsController(
        IAiProviderConfigurationService configurationService,
        IAiProviderRegistry providerRegistry)
    {
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
        var list = await _configurationService.GetConfigurationsByTenantAsync(tenantId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("tenant/{tenantId:guid}/{id:guid}")]
    public async Task<ActionResult<AiProviderConfigurationResponseDto>> GetConfigurationById(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
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
        var provider = await _configurationService.GetRuntimeProviderForTenantAsync(tenantId, preferredProvider, cancellationToken);
        var health = await provider.HealthAsync(cancellationToken);
        return Ok(health);
    }
}
