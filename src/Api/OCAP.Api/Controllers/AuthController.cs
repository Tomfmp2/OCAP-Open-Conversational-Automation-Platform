using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Security;
using OCAP.Security.Abstractions;
using OCAP.Security.Application.UseCases;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting(OCAP.Api.Configuration.RateLimitingSettings.AuthPolicy)]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserUseCase _authUseCase;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly OCAPDbContext _dbContext;

    public AuthController(
        AuthenticateUserUseCase authUseCase,
        IRefreshTokenService refreshTokenService,
        IJwtTokenService jwtTokenService,
        OCAPDbContext dbContext)
    {
        _authUseCase = authUseCase ?? throw new ArgumentNullException(nameof(authUseCase));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>Autentica usuario y emite access + refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var result = await _authUseCase.ExecuteAsync(request.Email, request.Password, ip, cancellationToken);

        if (result == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas",
                Detail = "Email o contraseña incorrectos."
            });
        }

        return Ok(new LoginResponseDto(
            result.AccessToken, result.RefreshToken, result.UserId, result.TenantId, result.Email, result.RoleName,
            result.MfaRequired, result.MfaToken));
    }

    /// <summary>
    /// Define la contraseña con un enlace de un solo uso (invitación al equipo o recuperación). Cierra las sesiones
    /// abiertas de la cuenta.
    /// </summary>
    [HttpPost("password/setup")]
    public async Task<IActionResult> SetupPassword(
        [FromBody] PasswordSetupRequestDto request,
        [FromServices] OCAP.Api.Team.TeamService team,
        CancellationToken cancellationToken)
    {
        if (OCAP.Api.Security.PasswordRules.Validate(request.NewPassword) is { } rule)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: rule);

        return await team.CompletePasswordSetupAsync(request.Token, request.NewPassword, cancellationToken)
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Enlace inválido",
                detail: "El enlace no es válido, ya se usó o venció. Pide uno nuevo a un administrador.");
    }

    /// <summary>"Olvidé mi contraseña": envía un enlace si hay correo configurado. Misma respuesta exista o no la cuenta.</summary>
    [HttpPost("password/forgot")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        [FromServices] OCAP.Api.Team.TeamService team,
        CancellationToken cancellationToken)
    {
        await team.RequestPasswordResetAsync(request.Email, cancellationToken);
        return Ok(new { emailConfigured = team.EmailConfigured });
    }

    /// <summary>Si el servidor puede enviar correos (cambia lo que ofrece el panel en "Olvidé mi contraseña").</summary>
    [HttpGet("password/options")]
    [Microsoft.AspNetCore.RateLimiting.DisableRateLimiting]
    public IActionResult PasswordOptions([FromServices] OCAP.Api.Team.TeamService team) =>
        Ok(new { emailConfigured = team.EmailConfigured, minLength = OCAP.Api.Security.PasswordRules.MinLength });

    /// <summary>Rota refresh token y emite nuevo access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var newRefreshToken = await _refreshTokenService.ValidateAndRotateRefreshTokenAsync(request.RefreshToken, null, cancellationToken);
        if (newRefreshToken == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Refresh inválido",
                Detail = "Refresh token inválido o expirado."
            });
        }

        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == newRefreshToken.UserId && u.IsActive, cancellationToken);

        // Una cuenta bloqueada no renueva la sesión aunque tenga un refresh token vigente.
        if (user is null || user.IsLockedOut(DateTime.UtcNow))
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(newRefreshToken.Token, cancellationToken: cancellationToken);
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Usuario inválido",
                Detail = "Usuario no encontrado o inactivo."
            });
        }

        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId && t.IsActive, cancellationToken);

        if (tenant is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Tenant inválido",
                Detail = "Tenant no encontrado o inactivo."
            });
        }

        var userRole = await _dbContext.UserRoles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id, cancellationToken);

        if (userRole is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sin roles",
                Detail = "El usuario no tiene roles asignados."
            });
        }

        var role = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == userRole.RoleId, cancellationToken);

        if (role is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Rol inválido",
                Detail = "Rol de usuario no encontrado."
            });
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, tenant, role, role.Permissions);
        return Ok(new LoginResponseDto(newAccessToken, newRefreshToken.Token, user.Id, tenant.Id, user.Email, role.Name));
    }

    /// <summary>Revoca el refresh token de la sesión actual.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken: cancellationToken);
        }

        return Ok(new { message = "Sesión cerrada correctamente." });
    }
}
