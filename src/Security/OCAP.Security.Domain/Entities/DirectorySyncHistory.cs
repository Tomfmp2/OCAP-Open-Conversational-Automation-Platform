namespace OCAP.Security.Domain.Entities;

// Historial detallado e inmutable de ejecuciones de sincronización de directorio (CAP-19).
public class DirectorySyncHistory
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid JobId { get; private set; }
    public string SyncType { get; private set; } = "Full"; // Full, Incremental, Delta
    public string Status { get; private set; } = "Completed";
    public int UsersCreated { get; private set; }
    public int UsersUpdated { get; private set; }
    public int UsersDeprovisioned { get; private set; }
    public int GroupsSynced { get; private set; }
    public string? ErrorLog { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; }

    private DirectorySyncHistory() { }

    public DirectorySyncHistory(
        Guid id,
        Guid tenantId,
        Guid jobId,
        string syncType,
        string status,
        int usersCreated,
        int usersUpdated,
        int usersDeprovisioned,
        int groupsSynced,
        string? errorLog = null)
    {
        Id = id;
        TenantId = tenantId;
        JobId = jobId;
        SyncType = syncType;
        Status = status;
        UsersCreated = usersCreated;
        UsersUpdated = usersUpdated;
        UsersDeprovisioned = usersDeprovisioned;
        GroupsSynced = groupsSynced;
        ErrorLog = errorLog;
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
