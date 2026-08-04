namespace OCAP.Agents.Abstractions.Contracts;

/// <summary>
/// Provee un snapshot textual del estado real del tenant para el agente madre.
/// </summary>
public interface IOcapSystemContextProvider
{
    Task<string> GetTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
