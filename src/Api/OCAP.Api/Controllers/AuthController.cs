using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;
using OCAP.Security.Application.UseCases;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserUseCase _authUseCase;

    public AuthController(AuthenticateUserUseCase authUseCase)
    {
        _authUseCase = authUseCase ?? throw new ArgumentNullException(nameof(authUseCase));
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
    public ActionResult<LoginResponseDto> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        return Ok(new LoginResponseDto("new_jwt_access_token", request.RefreshToken, Guid.NewGuid(), Guid.NewGuid(), "user@ocap.io", "Admin"));
    }

    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new { message = "Sesión cerrada correctamente." });
}
