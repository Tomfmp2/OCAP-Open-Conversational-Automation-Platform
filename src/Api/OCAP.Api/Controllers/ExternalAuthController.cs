using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST para autenticación y gestión de proveedores de identidad externos (CAP-15).
[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private readonly IExternalAuthenticationService _externalAuthService;
    private readonly ITenantContext _tenantContext;

    public ExternalAuthController(
        IExternalAuthenticationService externalAuthService,
        ITenantContext tenantContext)
    {
        _externalAuthService = externalAuthService ?? throw new ArgumentNullException(nameof(externalAuthService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ExternalProviderInfoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnabledProviders(CancellationToken cancellationToken)
    {
        var providers = await _externalAuthService.GetEnabledProvidersAsync(cancellationToken);
        return Ok(providers);
    }

    [HttpGet("challenge/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExternalAuthChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Challenge([FromRoute] string provider, [FromQuery] string? redirectUri, CancellationToken cancellationToken)
    {
        try
        {
            var fallbackRedirect = redirectUri ?? $"{Request.Scheme}://{Request.Host}/api/auth/external/callback/{provider}";
            var challenge = await _externalAuthService.InitiateChallengeAsync(provider, fallbackRedirect, _tenantContext.TenantId, cancellationToken);
            return Ok(challenge);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("callback/{provider}")]
    [HttpPost("callback/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExternalAuthLoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Callback(
        [FromRoute] string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? redirectUri,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { error = "El código de autorización es requerido." });
        }

        var fallbackRedirect = redirectUri ?? $"{Request.Scheme}://{Request.Host}/api/auth/external/callback/{provider}";
        var callbackRequest = new ExternalAuthCallbackRequestDto(provider, code, state ?? string.Empty, fallbackRedirect);

        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : Guid.NewGuid();
        var result = await _externalAuthService.ProcessCallbackAsync(callbackRequest, tenantId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("linked")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLinkedProviders(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var providers = await _externalAuthService.GetLinkedProvidersAsync(_tenantContext.TenantId, userId, cancellationToken);
        return Ok(providers);
    }

    [HttpDelete("linked/{provider}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnlinkProvider([FromRoute] string provider, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var unlinked = await _externalAuthService.UnlinkProviderAsync(_tenantContext.TenantId, userId, provider, cancellationToken);
        if (!unlinked)
        {
            return BadRequest(new { error = $"No se pudo desvincular el proveedor '{provider}'." });
        }

        return Ok(new { message = $"Proveedor '{provider}' desvinculado exitosamente." });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("user_id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : Guid.Empty;
    }
}
