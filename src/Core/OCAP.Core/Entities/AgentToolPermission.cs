namespace OCAP.Core.Entities;

// Entidad de persistencia que asocia permisos explícitos de ejecución a un agente.
public class AgentToolPermission
{
    public Guid Id { private set; get; }
    public Guid AgentId { private set; get; }
    public string PermissionName { private set; get; } = string.Empty;
    public bool IsAllowed { private set; get; }
    public DateTime CreatedAt { private set; get; }

    private AgentToolPermission() { } // Constructor ORM

    public AgentToolPermission(Guid id, Guid agentId, string permissionName, bool isAllowed)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede ser vacío.", nameof(id));
        if (agentId == Guid.Empty) throw new ArgumentException("El ID de agente no puede ser vacío.", nameof(agentId));
        
        Id = id;
        AgentId = agentId;
        PermissionName = permissionName ?? throw new ArgumentNullException(nameof(permissionName));
        IsAllowed = isAllowed;
        CreatedAt = DateTime.UtcNow;
    }
}
