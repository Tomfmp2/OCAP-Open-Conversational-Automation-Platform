namespace OCAP.Security.Abstractions;

// Entidad/Política que representa los permisos concedidos y denegados para un Agente.
public class AgentPermissionPolicy
{
    // Identificador único del agente evaluado.
    public Guid AgentId { get; }

    // Conjunto de permisos explícitamente permitidos (ej. "Calendar.Create", "Gmail.Send").
    public HashSet<string> AllowedPermissions { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Conjunto de permisos explícitamente denegados (ej. "Drive.Delete").
    public HashSet<string> DeniedPermissions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public AgentPermissionPolicy(Guid agentId)
    {
        if (agentId == Guid.Empty) throw new ArgumentException("El ID de agente no puede ser vacío.", nameof(agentId));
        AgentId = agentId;
    }

    // Otorga un permiso explícito al agente.
    public void Allow(string permission)
    {
        if (!string.IsNullOrWhiteSpace(permission))
        {
            DeniedPermissions.Remove(permission);
            AllowedPermissions.Add(permission);
        }
    }

    // Deniega explícitamente un permiso al agente (toma precedencia sobre las autorizaciones).
    public void Deny(string permission)
    {
        if (!string.IsNullOrWhiteSpace(permission))
        {
            AllowedPermissions.Remove(permission);
            DeniedPermissions.Add(permission);
        }
    }

    // Evalúa si un permiso específico está permitido según la política.
    public bool IsPermissionAllowed(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return false;
        if (DeniedPermissions.Contains(permission)) return false;
        return AllowedPermissions.Contains(permission);
    }
}
