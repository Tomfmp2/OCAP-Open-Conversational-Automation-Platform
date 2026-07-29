using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST de gestión del perfil de usuario autenticado (CAP-16).
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly ITenantContext _tenantContext;

    public ProfileController(IUserManagementService userService, ITenantContext tenantContext)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var profile = await _userService.GetUserByIdAsync(_tenantContext.TenantId, userId, cancellationToken);
        if (profile == null) return NotFound(new { error = "Perfil de usuario no encontrado." });

        return Ok(profile);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var success = await _userService.ChangePasswordAsync(_tenantContext.TenantId, userId, request, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo cambiar la contraseña. Verifique su contraseña actual." });

        return Ok(new { message = "Contraseña modificada exitosamente." });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("user_id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : Guid.Empty;
    }
}
