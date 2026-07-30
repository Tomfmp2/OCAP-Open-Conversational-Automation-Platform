using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

/// <summary>
/// Contexto multi-tenant basado en claims autenticados (fail-safe).
/// En producción: sin claim de tenant → Guid.Empty (no inventar tenants).
/// En Development/Testing anónimo: X-Tenant-ID o tenant de prueba controlado.
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
            // Jobs sin HTTP: tenant por defecto + BypassTenantFilters.
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

                // Fail-safe: autenticado sin claim → no aceptar header spoofable ni inventar tenant.
                return Guid.Empty;
            }

            if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            {
                if (httpContext.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue)
                    && !string.IsNullOrWhiteSpace(headerValue)
                    && Guid.TryParse(headerValue, out var headerTenantId)
                    && headerTenantId != Guid.Empty)
                {
                    return headerTenantId;
                }

                return DefaultTenantId;
            }

            return Guid.Empty;
        }
    }

    public string TenantName => TenantId == Guid.Empty ? "Unresolved" : $"Tenant-{TenantId:N}";

    public bool IsResolved => _httpContextAccessor.HttpContext != null && TenantId != Guid.Empty;

    public bool BypassTenantFilters => _httpContextAccessor.HttpContext == null;
}
