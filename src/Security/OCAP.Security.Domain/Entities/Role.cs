namespace OCAP.Security.Domain.Entities;

// Entidad que representa un Rol dentro del sistema de control de acceso RBAC.
public class Role
{
    // Identificador único del rol.
    public Guid Id { get; private set; }

    // Tenant al que pertenece el rol (Guid.Empty para roles globales de sistema).
    public Guid TenantId { get; private set; }

    // Nombre del rol (ej. Admin, Operator, Viewer).
    public string Name { get; private set; } = string.Empty;

    // Descripción del propósito y responsabilidades del rol.
    public string Description { get; private set; } = string.Empty;

    // Lista de códigos de permisos asignados a este rol.
    public List<string> Permissions { get; private set; } = new();

    private Role() { } // Constructor ORM.

    // Inicializa un rol con sus permisos asociados.
    public Role(Guid id, Guid tenantId, string name, string description, IEnumerable<string>? permissions = null)
    {
        Id = id;
        TenantId = tenantId;
        Name = name.Trim();
        Description = description ?? string.Empty;
        if (permissions != null)
        {
            Permissions.AddRange(permissions);
        }
    }

    // Agrega un permiso al rol si no existe previamente.
    public void AddPermission(string permissionCode)
    {
        if (!Permissions.Contains(permissionCode))
        {
            Permissions.Add(permissionCode);
        }
    }

    // Remueve un permiso del rol.
    public void RemovePermission(string permissionCode) => Permissions.Remove(permissionCode);
}
