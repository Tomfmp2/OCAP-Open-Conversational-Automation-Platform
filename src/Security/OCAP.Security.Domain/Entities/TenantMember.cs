namespace OCAP.Security.Domain.Entities;

// Entidad de membresía que vincula un usuario con un tenant y le asigna un rol dentro de la organización.
public class TenantMember
{
    // Identificador único del registro de membresía.
    public Guid Id { get; private set; }

    // Identificador del tenant al que pertenece la membresía.
    public Guid TenantId { get; private set; }

    // Identificador del usuario miembro.
    public Guid UserId { get; private set; }

    // Identificador del rol asignado dentro del tenant.
    public Guid RoleId { get; private set; }

    // Fecha de incorporación al tenant.
    public DateTime JoinedAtUtc { get; private set; }

    private TenantMember() { } // Constructor ORM.

    // Crea una nueva relación de membresía de usuario en un tenant.
    public TenantMember(Guid id, Guid tenantId, Guid userId, Guid roleId)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        JoinedAtUtc = DateTime.UtcNow;
    }

    // Asigna un nuevo rol al miembro dentro de la organización.
    public void AssignRole(Guid newRoleId) => RoleId = newRoleId;
}
