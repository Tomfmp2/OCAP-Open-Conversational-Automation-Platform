namespace OCAP.Security.Domain.Entities;

// Entidad de auditoría de seguridad que registra eventos críticos del sistema.
public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public DateTime TimestampUtc { get; private set; }
    public bool Success { get; private set; }

    private AuditLog() { } // Constructor ORM.

    public AuditLog(Guid id, Guid tenantId, Guid userId, string action, string details, string ipAddress, bool success)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Action = action;
        Details = details ?? string.Empty;
        IpAddress = ipAddress ?? "Unknown";
        Success = success;
        TimestampUtc = DateTime.UtcNow;
    }
}
