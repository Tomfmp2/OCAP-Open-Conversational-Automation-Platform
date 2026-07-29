namespace OCAP.Security.Domain.Entities;

// Entidad de relación N:M entre Grupos y Roles dentro de un Tenant (CAP-16).
public class GroupRole
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid RoleId { get; private set; }

    private GroupRole() { }

    public GroupRole(Guid id, Guid tenantId, Guid groupId, Guid roleId)
    {
        Id = id;
        TenantId = tenantId;
        GroupId = groupId;
        RoleId = roleId;
    }
}
