namespace OCAP.Security.Domain.Entities;

// Entidad de Grupo de Usuarios para gestión masiva de membresías y roles (CAP-16).
public class Group
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private Group() { } // Constructor ORM.

    public Group(Guid id, Guid tenantId, string name, string? description = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de grupo no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre del grupo es obligatorio.", nameof(name));

        Id = id;
        TenantId = tenantId;
        Name = name.Trim();
        Description = description ?? string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre del grupo es obligatorio.", nameof(name));
        Name = name.Trim();
        Description = description ?? string.Empty;
    }
}
