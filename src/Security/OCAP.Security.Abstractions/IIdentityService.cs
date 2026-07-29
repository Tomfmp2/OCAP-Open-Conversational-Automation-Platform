using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato unificado de gestión de Identidad, Roles (RBAC), Permisos y Claims multi-tenant compatibles con OpenIddict / OIDC.
public interface IIdentityService
{
    // Obtiene los roles asignados a un usuario dentro de un tenant.
    Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    // Obtiene los permisos acumulados asignados a un usuario a través de sus roles en un tenant.
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    // Obtiene los claims personalizados asignados a un usuario en un tenant.
    Task<IReadOnlyList<UserClaim>> GetUserClaimsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    // Asigna un rol a un usuario en un tenant.
    Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default);

    // Remueve un rol asignado a un usuario en un tenant.
    Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default);

    // Agrega o actualiza un claim personalizado de usuario en un tenant.
    Task<UserClaim> AddOrUpdateUserClaimAsync(Guid userId, Guid tenantId, string claimType, string claimValue, CancellationToken cancellationToken = default);

    // Elimina un claim de usuario.
    Task<bool> RemoveUserClaimAsync(Guid userId, Guid tenantId, string claimType, CancellationToken cancellationToken = default);

    // Evalúa si un usuario cuenta con un permiso específico en un tenant.
    Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string permissionCode, CancellationToken cancellationToken = default);

    // Evalúa si un usuario pertenece a un rol específico en un tenant.
    Task<bool> HasRoleAsync(Guid userId, Guid tenantId, string roleName, CancellationToken cancellationToken = default);
}
