using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Security;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Application.UseCases;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var result = await _authUseCase.ExecuteAsync(request.Email, request.Password, ip, cancellationToken);

        if (result == null) return Unauthorized(new { message = "Credenciales inválidas." });

        return Ok(new LoginResponseDto(result.AccessToken, result.RefreshToken, result.UserId, result.TenantId, result.Email, result.RoleName));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var newRefreshToken = await _refreshTokenService.ValidateAndRotateRefreshTokenAsync(request.RefreshToken, null, cancellationToken);
        if (newRefreshToken == null)
        {
            return Unauthorized(new { message = "Refresh token inválido o expirado." });
        }

        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == newRefreshToken.UserId, cancellationToken);
        
        Tenant tenant;
        Role? role;
        
        if (user == null)
        {
            var tenantId = Guid.NewGuid();
            var userId = newRefreshToken.UserId;
            user = new UserIdentity(userId, tenantId, "user@ocap.io", "", "", "Usuario Administrador");
            tenant = new Tenant(tenantId, "Organización Principal", "org-principal");
            role = new Role(Guid.NewGuid(), tenantId, "Admin", "Administrador total", new List<string> { "Conversation.Read", "Conversation.Write" });
        }
        else
        {
            tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken)
                     ?? new Tenant(user.TenantId, "Organización Principal", "org-principal");
            
            // Auth bootstrap: resolve role for the user across tenants (token already validated).
            var userRole = await _dbContext.UserRoles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ur => ur.UserId == user.Id, cancellationToken);
            role = userRole != null 
                ? await _dbContext.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.Id == userRole.RoleId, cancellationToken)
                : null;

            if (role == null)
            {
                role = new Role(Guid.NewGuid(), tenant.Id, "Admin", "Administrador", new List<string> { "Conversation.Read", "Conversation.Write" });
            }
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, tenant, role, role.Permissions);

        return Ok(new LoginResponseDto(newAccessToken, newRefreshToken.Token, user.Id, tenant.Id, user.Email, role.Name));
    }

    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new { message = "Sesión cerrada correctamente." });
}
