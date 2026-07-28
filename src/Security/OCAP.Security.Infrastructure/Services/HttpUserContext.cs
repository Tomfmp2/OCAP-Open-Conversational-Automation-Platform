using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

/// <summary>
/// Implementación del contexto de usuario basada en HttpContext.
/// </summary>
public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return Guid.Empty;

            var claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("sub")?.Value
                             ?? user.FindFirst("user_id")?.Value
                             ?? user.FindFirst("UserId")?.Value;

            if (!string.IsNullOrWhiteSpace(claimValue) && Guid.TryParse(claimValue, out var userId))
            {
                return userId;
            }

            return Guid.Empty;
        }
    }

    public string UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Name)?.Value
                   ?? user?.FindFirst("name")?.Value
                   ?? user?.Identity?.Name
                   ?? "System";
        }
    }

    public string Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                   ?? user?.FindFirst("email")?.Value
                   ?? string.Empty;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
