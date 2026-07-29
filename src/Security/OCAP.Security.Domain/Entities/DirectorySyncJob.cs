namespace OCAP.Security.Domain.Entities;

// Entidad para registro de trabajos de sincronización de directorio (LDAP/SCIM) (CAP-19).
public class DirectorySyncJob
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ProviderType { get; private set; } = "LDAP"; // LDAP, SCIM, ActiveDirectory
    public string Status { get; private set; } = "Idle"; // Idle, Running, Completed, Failed
    public DateTime? LastSyncAtUtc { get; private set; }
    public int TotalUsersSynced { get; private set; }
    public int TotalGroupsSynced { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private DirectorySyncJob() { }

    public DirectorySyncJob(Guid id, Guid tenantId, string providerType)
    {
        Id = id;
        TenantId = tenantId;
        ProviderType = providerType;
        Status = "Idle";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void StartSync()
    {
        Status = "Running";
        LastErrorMessage = null;
    }

    public void CompleteSync(int usersSynced, int groupsSynced)
    {
        Status = "Completed";
        LastSyncAtUtc = DateTime.UtcNow;
        TotalUsersSynced = usersSynced;
        TotalGroupsSynced = groupsSynced;
        LastErrorMessage = null;
    }

    public void FailSync(string error)
    {
        Status = "Failed";
        LastErrorMessage = error;
    }
}
