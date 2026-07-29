using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato para motor de sincronización (Full, Incremental, Delta) de directorios (CAP-19).
public interface IDirectorySyncEngine
{
    Task<SyncStatusDto> TriggerSyncJobAsync(Guid tenantId, string providerType = "LDAP", string syncType = "Full", CancellationToken cancellationToken = default);
    Task<SyncStatusDto?> GetSyncStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<SyncHistoryDto>> GetSyncHistoryAsync(Guid tenantId, int top = 50, CancellationToken cancellationToken = default);
}
