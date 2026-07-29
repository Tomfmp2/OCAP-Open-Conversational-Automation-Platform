using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST para registro, aserción y gestión de dispositivos WebAuthn / Passkeys (CAP-17).
[ApiController]
[Route("api/auth/webauthn")]
[Authorize]
public class WebAuthnController : ControllerBase
{
    private readonly IWebAuthnService _webAuthnService;
    private readonly ITenantContext _tenantContext;

    public WebAuthnController(IWebAuthnService webAuthnService, ITenantContext tenantContext)
    {
        _webAuthnService = webAuthnService ?? throw new ArgumentNullException(nameof(webAuthnService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpPost("register/options")]
    [ProducesResponseType(typeof(WebAuthnRegisterOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegisterOptions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();
        if (userId == Guid.Empty) return Unauthorized();

        var options = await _webAuthnService.GenerateRegistrationOptionsAsync(_tenantContext.TenantId, userId, email, cancellationToken);
        return Ok(options);
    }

    [HttpPost("register/complete")]
    [ProducesResponseType(typeof(WebAuthnDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteRegistration([FromBody] WebAuthnRegisterRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var device = await _webAuthnService.CompleteRegistrationAsync(_tenantContext.TenantId, userId, request, cancellationToken);
            return Ok(device);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("assertion/options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WebAuthnAssertionOptionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssertionOptions([FromQuery] Guid tenantId, [FromQuery] string email, CancellationToken cancellationToken)
    {
        var options = await _webAuthnService.GenerateAssertionOptionsAsync(tenantId, email, cancellationToken);
        return Ok(options);
    }

    [HttpPost("assertion/complete")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteAssertion([FromQuery] Guid tenantId, [FromBody] WebAuthnAssertionRequestDto request, CancellationToken cancellationToken)
    {
        var success = await _webAuthnService.CompleteAssertionAsync(tenantId, request, cancellationToken);
        if (!success) return BadRequest(new { error = "Verificación de credencial Passkey no válida." });

        return Ok(new { message = "Autenticación por Passkey completada exitosamente." });
    }

    [HttpGet("devices")]
    [ProducesResponseType(typeof(IReadOnlyList<WebAuthnDeviceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevices(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var devices = await _webAuthnService.GetRegisteredDevicesAsync(_tenantContext.TenantId, userId, cancellationToken);
        return Ok(devices);
    }

    [HttpDelete("devices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDevice([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var deleted = await _webAuthnService.DeleteDeviceAsync(_tenantContext.TenantId, userId, id, cancellationToken);
        if (!deleted) return BadRequest(new { error = "No se pudo eliminar el dispositivo Passkey especificado." });

        return Ok(new { message = "Dispositivo Passkey eliminado exitosamente." });
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
