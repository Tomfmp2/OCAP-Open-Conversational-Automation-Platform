namespace OCAP.Security.Domain.Entities;

// Entidad que representa la asignación de un rol a un usuario dentro de un tenant.
public class UserRole
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private UserRole() { } // ORM

    public UserRole(Guid id, Guid userId, Guid roleId, Guid tenantId)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede ser vacío.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));
        if (roleId == Guid.Empty) throw new ArgumentException("El RoleId no puede ser vacío.", nameof(roleId));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));

        Id = id;
        UserId = userId;
        RoleId = roleId;
        TenantId = tenantId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
