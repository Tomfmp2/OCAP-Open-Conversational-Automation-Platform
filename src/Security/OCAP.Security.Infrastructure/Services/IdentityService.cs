using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de gestión de Identidad, Roles, Permisos y Claims con aislamiento Multi-Tenant e integración EF Core.
public class IdentityService : IIdentityService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<IdentityService>? _logger;

    public IdentityService(
        OCAPDbContext dbContext,
        ISecurityAuditService auditService,
        ILogger<IdentityService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger;
    }

    public async Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0) return Array.Empty<Role>();

        var roles = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var roles = await GetUserRolesAsync(userId, tenantId, cancellationToken);
        var permissions = roles
            .SelectMany(r => r.Permissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return permissions;
    }

    public async Task<IReadOnlyList<UserClaim>> GetUserClaimsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var claims = await _dbContext.UserClaims
            .Where(c => c.UserId == userId && c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return claims;
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantId, cancellationToken);

        if (exists) return true;

        var userRole = new UserRole(Guid.NewGuid(), userId, roleId, tenantId);
        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Rol {RoleId} asignado al usuario {UserId} en Tenant {TenantId}", roleId, userId, tenantId);

        await _auditService.LogSecurityEventAsync(
            tenantId, userId, "Identity.RoleAssigned", $"Asignación de Rol ID {roleId} a usuario", "IdentityService", true, cancellationToken);

        return true;
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantId, cancellationToken);

        if (userRole == null) return false;

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Rol {RoleId} removido del usuario {UserId} en Tenant {TenantId}", roleId, userId, tenantId);

        await _auditService.LogSecurityEventAsync(
            tenantId, userId, "Identity.RoleRemoved", $"Remoción de Rol ID {roleId} a usuario", "IdentityService", true, cancellationToken);

        return true;
    }

    public async Task<UserClaim> AddOrUpdateUserClaimAsync(Guid userId, Guid tenantId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(claimType)) throw new ArgumentException("El ClaimType es requerido.", nameof(claimType));

        var existingClaim = await _dbContext.UserClaims
            .FirstOrDefaultAsync(c => c.UserId == userId && c.TenantId == tenantId && c.ClaimType == claimType, cancellationToken);

        if (existingClaim != null)
        {
            existingClaim.UpdateValue(claimValue);
        }
        else
        {
            existingClaim = new UserClaim(Guid.NewGuid(), userId, tenantId, claimType, claimValue);
            _dbContext.UserClaims.Add(existingClaim);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation("Claim {ClaimType} actualizado para usuario {UserId} en Tenant {TenantId}", claimType, userId, tenantId);

        return existingClaim;
    }

    public async Task<bool> RemoveUserClaimAsync(Guid userId, Guid tenantId, string claimType, CancellationToken cancellationToken = default)
    {
        var claim = await _dbContext.UserClaims
            .FirstOrDefaultAsync(c => c.UserId == userId && c.TenantId == tenantId && c.ClaimType == claimType, cancellationToken);

        if (claim == null) return false;

        _dbContext.UserClaims.Remove(claim);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string permissionCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) return true;

        var permissions = await GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions.Contains("*") || permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> HasRoleAsync(Guid userId, Guid tenantId, string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return false;

        var roles = await GetUserRolesAsync(userId, tenantId, cancellationToken);
        return roles.Any(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }
}
