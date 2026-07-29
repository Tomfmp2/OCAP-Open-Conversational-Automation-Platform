using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST de administración de usuarios (CAP-16).
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly ITenantContext _tenantContext;

    public UsersController(IUserManagementService userService, ITenantContext tenantContext)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(_tenantContext.TenantId, cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (user == null) return NotFound(new { error = $"Usuario '{id}' no encontrado." });

        return Ok(user);
    }

    [HttpPost("invite")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.InviteUserAsync(_tenantContext.TenantId, request, cancellationToken);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var success = await _userService.ResetPasswordAsync(_tenantContext.TenantId, request, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo generar el restablecimiento de contraseña." });

        return Ok(new { message = "Se ha enviado la solicitud de restablecimiento de contraseña." });
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var success = await _userService.ChangePasswordAsync(_tenantContext.TenantId, userId, request, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo cambiar la contraseña. Verifique su contraseña actual." });

        return Ok(new { message = "Contraseña modificada exitosamente." });
    }

    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LockUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var success = await _userService.LockUserAsync(_tenantContext.TenantId, id, null, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo bloquear el usuario especificado." });

        return Ok(new { message = "Usuario bloqueado exitosamente." });
    }

    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlockUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var success = await _userService.UnlockUserAsync(_tenantContext.TenantId, id, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo desbloquear el usuario especificado." });

        return Ok(new { message = "Usuario desbloqueado exitosamente." });
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var success = await _userService.ActivateUserAsync(_tenantContext.TenantId, id, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo activar el usuario especificado." });

        return Ok(new { message = "Usuario activado exitosamente." });
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var success = await _userService.DeactivateUserAsync(_tenantContext.TenantId, id, cancellationToken);
        if (!success) return BadRequest(new { error = "No se pudo desactivar el usuario especificado." });

        return Ok(new { message = "Usuario desactivado exitosamente." });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("user_id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : Guid.Empty;
    }
}
