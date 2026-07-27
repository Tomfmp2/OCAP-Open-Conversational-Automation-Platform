using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace OCAP.Dashboard.Authentication;

// Proveedor de estado de autenticación personalizado para el control de acceso en el Dashboard frontend.
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Administrador OCAP"),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim("Organization", "OCAP Enterprise")
        }, "CustomAuth");

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
