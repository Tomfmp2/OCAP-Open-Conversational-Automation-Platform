using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para administración de grupos, membresías y asignación de roles (CAP-16).
public class GroupService : IGroupService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;

    public GroupService(OCAPDbContext dbContext, ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var groups = await _dbContext.Groups
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var counts = await _dbContext.UserGroups
            .Where(ug => ug.TenantId == tenantId)
            .GroupBy(ug => ug.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);

        return groups.Select(g => new GroupDto(g.Id, g.TenantId, g.Name, g.Description, g.CreatedAtUtc, counts.TryGetValue(g.Id, out var c) ? c : 0)).ToList();
    }

    public async Task<GroupDto?> GetGroupByIdAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);

        if (group == null) return null;

        var count = await _dbContext.UserGroups.CountAsync(ug => ug.TenantId == tenantId && ug.GroupId == groupId, cancellationToken);
        return new GroupDto(group.Id, group.TenantId, group.Name, group.Description, group.CreatedAtUtc, count);
    }

    public async Task<GroupDto> CreateGroupAsync(Guid tenantId, CreateGroupRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("El nombre del grupo es obligatorio.", nameof(request));

        var existing = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Name.ToLower() == request.Name.ToLower().Trim(), cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Ya existe un grupo con el nombre '{request.Name}'.");

        var group = new Group(Guid.NewGuid(), tenantId, request.Name, request.Description);
        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Group.Created", $"Grupo '{group.Name}' creado", "GroupService", true, cancellationToken);

        return new GroupDto(group.Id, group.TenantId, group.Name, group.Description, group.CreatedAtUtc, 0);
    }

    public async Task<bool> AddUserToGroupAsync(Guid tenantId, Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);
        if (group == null) return false;

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null) return false;

        var existing = await _dbContext.UserGroups.FirstOrDefaultAsync(ug => ug.TenantId == tenantId && ug.GroupId == groupId && ug.UserId == userId, cancellationToken);
        if (existing != null) return true;

        var userGroup = new UserGroup(Guid.NewGuid(), tenantId, userId, groupId);
        _dbContext.UserGroups.Add(userGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "Group.UserAdded", $"Usuario {userId} añadido al grupo '{group.Name}'", "GroupService", true, cancellationToken);

        return true;
    }

    public async Task<bool> RemoveUserFromGroupAsync(Guid tenantId, Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserGroups.FirstOrDefaultAsync(ug => ug.TenantId == tenantId && ug.GroupId == groupId && ug.UserId == userId, cancellationToken);
        if (existing == null) return false;

        _dbContext.UserGroups.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "Group.UserRemoved", $"Usuario {userId} removido del grupo {groupId}", "GroupService", true, cancellationToken);

        return true;
    }

    public async Task<bool> AssignRoleToGroupAsync(Guid tenantId, Guid groupId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);
        if (group == null) return false;

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == roleId, cancellationToken);
        if (role == null) return false;

        var existing = await _dbContext.GroupRoles.FirstOrDefaultAsync(gr => gr.TenantId == tenantId && gr.GroupId == groupId && gr.RoleId == roleId, cancellationToken);
        if (existing != null) return true;

        var groupRole = new GroupRole(Guid.NewGuid(), tenantId, groupId, roleId);
        _dbContext.GroupRoles.Add(groupRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Group.RoleAssigned", $"Rol '{role.Name}' asignado al grupo '{group.Name}'", "GroupService", true, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteGroupAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);
        if (group == null) return false;

        var userGroups = await _dbContext.UserGroups.Where(ug => ug.TenantId == tenantId && ug.GroupId == groupId).ToListAsync(cancellationToken);
        _dbContext.UserGroups.RemoveRange(userGroups);

        var groupRoles = await _dbContext.GroupRoles.Where(gr => gr.TenantId == tenantId && gr.GroupId == groupId).ToListAsync(cancellationToken);
        _dbContext.GroupRoles.RemoveRange(groupRoles);

        _dbContext.Groups.Remove(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Group.Deleted", $"Grupo '{group.Name}' eliminado", "GroupService", true, cancellationToken);

        return true;
    }
}
