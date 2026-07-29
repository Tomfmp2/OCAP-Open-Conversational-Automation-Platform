using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST para Enterprise Single Sign-On SAML 2.0 (CAP-18).
[ApiController]
[Route("api/auth/saml")]
public class SamlController : ControllerBase
{
    private readonly ISamlService _samlService;
    private readonly ITenantContext _tenantContext;

    public SamlController(ISamlService samlService, ITenantContext tenantContext)
    {
        _samlService = samlService ?? throw new ArgumentNullException(nameof(samlService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet("metadata")]
    [AllowAnonymous]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpMetadata([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var targetTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        var xml = await _samlService.GetSpMetadataXmlAsync(targetTenantId, cancellationToken);
        return Content(xml, "application/xml", System.Text.Encoding.UTF8);
    }

    [HttpPost("metadata/import")]
    [Authorize]
    [ProducesResponseType(typeof(SamlProviderConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportMetadata([FromBody] ImportMetadataRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _samlService.ImportIdpMetadataXmlAsync(_tenantContext.TenantId, request.MetadataXml, cancellationToken);
            return Ok(config);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("config")]
    [Authorize]
    [ProducesResponseType(typeof(SamlProviderConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var config = await _samlService.GetSamlProviderConfigAsync(_tenantContext.TenantId, cancellationToken);
        if (config == null) return NotFound(new { error = "Configuración SAML 2.0 no encontrada para el tenant." });

        return Ok(config);
    }

    [HttpPost("config")]
    [Authorize]
    [ProducesResponseType(typeof(SamlProviderConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveConfig([FromBody] SaveSamlProviderConfigDto request, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _samlService.SaveSamlProviderConfigAsync(_tenantContext.TenantId, request, cancellationToken);
            return Ok(config);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SamlLoginRedirectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateLogin([FromQuery] Guid tenantId, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            var redirect = await _samlService.InitiateSpLoginAsync(tenantId, returnUrl, cancellationToken);
            return Ok(redirect);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("acs")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SamlAuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssertionConsumerService([FromQuery] Guid tenantId, [FromForm] SamlAcsRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var authResult = await _samlService.ProcessAcsResponseAsync(tenantId, request, cancellationToken);
            return Ok(authResult);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("slo")]
    [HttpPost("slo")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SingleLogout([FromQuery] Guid tenantId, [FromQuery] string? SAMLRequest, [FromQuery] string? SAMLResponse, CancellationToken cancellationToken)
    {
        var rawData = SAMLRequest ?? SAMLResponse ?? string.Empty;
        await _samlService.ProcessSloAsync(tenantId, rawData, cancellationToken);
        return Ok(new { message = "Single Logout SAML 2.0 completado exitosamente." });
    }

    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var config = await _samlService.GetSamlProviderConfigAsync(_tenantContext.TenantId, cancellationToken);
        return Ok(new { isConfigured = config != null, isEnabled = config?.IsEnabled ?? false });
    }
}
