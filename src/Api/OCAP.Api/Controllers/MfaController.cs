using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST de Autenticación de Múltiples Factores (MFA / TOTP) (CAP-17).
[ApiController]
[Route("api/auth/mfa")]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly ITenantContext _tenantContext;

    public MfaController(IMfaService mfaService, ITenantContext tenantContext)
    {
        _mfaService = mfaService ?? throw new ArgumentNullException(nameof(mfaService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpPost("setup")]
    [ProducesResponseType(typeof(MfaSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();
        if (userId == Guid.Empty) return Unauthorized();

        var setup = await _mfaService.SetupMfaAsync(_tenantContext.TenantId, userId, email, cancellationToken);
        return Ok(setup);
    }

    [HttpPost("enable")]
    [ProducesResponseType(typeof(EnableMfaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var result = await _mfaService.EnableMfaAsync(_tenantContext.TenantId, userId, request.Code, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableMfa([FromBody] VerifyMfaRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var disabled = await _mfaService.DisableMfaAsync(_tenantContext.TenantId, userId, request.Code, cancellationToken);
        if (!disabled) return BadRequest(new { error = "No se pudo desactivar MFA. Verifique el código ingresado." });

        return Ok(new { message = "MFA desactivado exitosamente." });
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyMfa([FromQuery] Guid tenantId, [FromQuery] Guid userId, [FromBody] VerifyMfaRequestDto request, CancellationToken cancellationToken)
    {
        var verified = await _mfaService.VerifyMfaCodeAsync(tenantId, userId, request.Code, cancellationToken);
        if (!verified) return BadRequest(new { error = "El código MFA o de recuperación ingresado no es válido." });

        return Ok(new { message = "Verificación MFA exitosa." });
    }

    [HttpPost("recovery-codes/regenerate")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegenerateRecoveryCodes(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var codes = await _mfaService.RegenerateRecoveryCodesAsync(_tenantContext.TenantId, userId, cancellationToken);
            return Ok(codes);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User?.FindFirst("user_id") ?? User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : Guid.Empty;
    }

    private string GetCurrentUserEmail()
    {
        var claim = User?.FindFirst(ClaimTypes.Email) ?? User?.FindFirst("email");
        return claim?.Value ?? "user@ocap.io";
    }
}
