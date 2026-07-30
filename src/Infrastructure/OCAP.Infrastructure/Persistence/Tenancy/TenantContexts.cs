using OCAP.Security.Abstractions;

namespace OCAP.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Contexto de tenant para operaciones de sistema (background jobs, design-time, tests sin HTTP).
/// </summary>
public sealed class SystemTenantContext : ITenantContext
{
    public static readonly SystemTenantContext Instance = new();

    private SystemTenantContext()
    {
    }

    public Guid TenantId { get; init; } = Guid.Empty;

    public string TenantName => "System";

    public bool IsResolved => true;

    public bool BypassTenantFilters => true;
}

/// <summary>
/// Contexto de tenant fijo para tests de aislamiento y scopes explícitos.
/// </summary>
public sealed class FixedTenantContext : ITenantContext
{
    public FixedTenantContext(Guid tenantId, bool bypassTenantFilters = false)
    {
        if (tenantId == Guid.Empty && !bypassTenantFilters)
            throw new ArgumentException("TenantId cannot be empty when filters are enforced.", nameof(tenantId));

        TenantId = tenantId;
        BypassTenantFilters = bypassTenantFilters;
    }

    public Guid TenantId { get; }

    public string TenantName => $"Tenant-{TenantId:N}";

    public bool IsResolved => TenantId != Guid.Empty || BypassTenantFilters;

    public bool BypassTenantFilters { get; }
}
