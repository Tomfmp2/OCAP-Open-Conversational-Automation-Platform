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
public class HttpTenantContext : ITenantContext, ITenantScope
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Clave de HttpContext.Items: solo la escribe código del servidor, nunca llega del cliente.</summary>
    private const string TrustedTenantItemKey = "ocap.trusted-tenant-id";

    /// <summary>
    /// Fija el tenant de una petición sin sesión que el servidor ya autenticó por otra vía (p. ej. un webhook con
    /// firma verificada). A partir de aquí las consultas y escrituras de esa petición quedan aisladas en ese tenant.
    /// </summary>
    public static void UseTrustedTenant(HttpContext httpContext, Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("El tenant es obligatorio.", nameof(tenantId));
        httpContext.Items[TrustedTenantItemKey] = tenantId;
    }
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;
    private Guid? _scopedTenant;

    public void Use(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("El tenant es obligatorio.", nameof(tenantId));
        _scopedTenant = tenantId;
    }

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Guid TenantId
    {
        get
        {
            if (_scopedTenant is { } scoped) return scoped;

            var httpContext = _httpContextAccessor.HttpContext;
            // Jobs sin HTTP: tenant por defecto + BypassTenantFilters.
            if (httpContext == null) return DefaultTenantId;

            if (httpContext.Items.TryGetValue(TrustedTenantItemKey, out var trusted) && trusted is Guid trustedTenant)
            {
                return trustedTenant;
            }

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

    public bool IsResolved => (_scopedTenant.HasValue || _httpContextAccessor.HttpContext != null) && TenantId != Guid.Empty;

    public bool BypassTenantFilters => !_scopedTenant.HasValue && _httpContextAccessor.HttpContext == null;
}
