using OCAP.Api.Models.Security;
using Microsoft.AspNetCore.RateLimiting;
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
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

    /// <summary>
    /// Segundo paso del login: el desafío que devolvió /api/auth/login más un código TOTP o de recuperación.
    /// Antes aceptaba cualquier tenant y usuario por query y no daba sesión: servía para adivinar códigos ajenos.
    /// </summary>
    [EnableRateLimiting(OCAP.Api.Configuration.RateLimitingSettings.AuthPolicy)]
    [HttpPost("verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyMfa(
        [FromBody] CompleteMfaLoginRequestDto request,
        [FromServices] OCAP.Security.Application.UseCases.AuthenticateUserUseCase authentication,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var result = await authentication.CompleteMfaAsync(request.MfaToken, request.Code, ip, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Código inválido",
                Detail = "El código no es válido o el inicio de sesión expiró. Vuelve a ingresar tu contraseña si pasaron más de 5 minutos."
            });
        }

        return Ok(new LoginResponseDto(result.AccessToken, result.RefreshToken, result.UserId, result.TenantId, result.Email, result.RoleName));
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
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
