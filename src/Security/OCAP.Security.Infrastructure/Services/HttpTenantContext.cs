using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

// Implementación de contexto multi-tenant que extrae y valida la identidad del Tenant desde la petición HTTP actual o Claims.
public class HttpTenantContext : ITenantContext
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return DefaultTenantId;

            // 1. Intentar obtener TenantId desde los claims del usuario autenticado
            var claimValue = httpContext.User?.FindFirst("tenant_id")?.Value
                             ?? httpContext.User?.FindFirst("TenantId")?.Value
                             ?? httpContext.User?.FindFirst(ClaimTypes.GroupSid)?.Value;

            if (!string.IsNullOrWhiteSpace(claimValue) && Guid.TryParse(claimValue, out var claimTenantId) && claimTenantId != Guid.Empty)
            {
                return claimTenantId;
            }

            // 2. Intentar obtener desde el Header HTTP 'X-Tenant-ID'
            if (httpContext.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue) &&
                !string.IsNullOrWhiteSpace(headerValue) &&
                Guid.TryParse(headerValue, out var headerTenantId) &&
                headerTenantId != Guid.Empty)
            {
                return headerTenantId;
            }

            // 3. Devolver el Tenant por defecto para entornos de desarrollo/pruebas
            return DefaultTenantId;
        }
    }

    public string TenantName => $"Tenant-{TenantId:N}";

    public bool IsResolved => _httpContextAccessor.HttpContext != null && TenantId != Guid.Empty;
}
