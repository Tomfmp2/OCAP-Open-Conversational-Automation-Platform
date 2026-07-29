namespace OCAP.Security.Domain.Entities;

// Entidad de relación N:M entre Usuarios y Grupos dentro de un Tenant (CAP-16).
public class UserGroup
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GroupId { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    private UserGroup() { }

    public UserGroup(Guid id, Guid tenantId, Guid userId, Guid groupId)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        GroupId = groupId;
        JoinedAtUtc = DateTime.UtcNow;
    }
}
