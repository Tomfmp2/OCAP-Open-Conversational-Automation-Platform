namespace OCAP.Security.Domain.Entities;

// Entidad de auditoría para el seguimiento de sesiones activas de usuario e IP.
public class UserSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public DateTime LoginAtUtc { get; private set; }
    public DateTime? LogoutAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;

    private UserSession() { } // Constructor ORM.

    public UserSession(Guid id, Guid userId, Guid tenantId, string ipAddress, string userAgent)
    {
        Id = id;
        UserId = userId;
        TenantId = tenantId;
        IpAddress = ipAddress ?? "Unknown";
        UserAgent = userAgent ?? "Unknown";
        LoginAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    // Cierra la sesión activa.
    public void TerminateSession()
    {
        LogoutAtUtc = DateTime.UtcNow;
        IsActive = false;
    }
}
