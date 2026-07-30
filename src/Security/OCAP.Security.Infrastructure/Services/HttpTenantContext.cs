using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

/// <summary>
/// Contexto multi-tenant basado en claims autenticados.
/// En producción el header X-Tenant-ID no puede suplantar el tenant de un usuario autenticado
/// ni fijar un tenant arbitrario en peticiones anónimas.
/// </summary>
public class HttpTenantContext : ITenantContext
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Guid TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return DefaultTenantId;

            var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated == true;

            if (isAuthenticated)
            {
                var claimValue = httpContext.User?.FindFirst("tenant_id")?.Value
                                 ?? httpContext.User?.FindFirst("TenantId")?.Value;

                if (!string.IsNullOrWhiteSpace(claimValue)
                    && Guid.TryParse(claimValue, out var claimTenantId)
                    && claimTenantId != Guid.Empty)
                {
                    return claimTenantId;
                }

                // Usuario autenticado sin claim de tenant: no aceptar header spoofable.
                return DefaultTenantId;
            }

            // Anónimo: solo permitir X-Tenant-ID en Development/Testing para escenarios locales.
            if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            {
                if (httpContext.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue)
                    && !string.IsNullOrWhiteSpace(headerValue)
                    && Guid.TryParse(headerValue, out var headerTenantId)
                    && headerTenantId != Guid.Empty)
                {
                    return headerTenantId;
                }
            }

            return DefaultTenantId;
        }
    }

    public string TenantName => $"Tenant-{TenantId:N}";

    public bool IsResolved => _httpContextAccessor.HttpContext != null && TenantId != Guid.Empty;
}
