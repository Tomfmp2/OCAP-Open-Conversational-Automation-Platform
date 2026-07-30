using OCAP.Security.Abstractions;

namespace OCAP.Api.Security;

/// <summary>
/// Helpers fail-safe para contexto de tenant/usuario en controladores.
/// </summary>
public static class TenantSecurity
{
    public static Guid RequireTenantId(ITenantContext tenantContext)
    {
        if (tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Contexto de tenant requerido.");
        }

        return tenantContext.TenantId;
    }

    public static Guid RequireUserId(IUserContext userContext)
    {
        if (!userContext.IsAuthenticated || userContext.UserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Usuario autenticado requerido.");
        }

        return userContext.UserId;
    }
}
