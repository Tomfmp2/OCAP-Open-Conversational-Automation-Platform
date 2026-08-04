using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OCAP.Security.Abstractions;

namespace OCAP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Enforce tenant isolation on write paths: assign TenantId on insert,
/// reject cross-tenant writes, and block TenantId mutations.
/// Login/anonymous flows may persist when every entity already carries TenantId.
/// </summary>
public sealed class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    public const string TenantIdPropertyName = "TenantId";

    private readonly ITenantContext _tenantContext;

    public TenantSaveChangesInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        EnforceTenantRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EnforceTenantRules(DbContext? context)
    {
        if (context is null) return;
        if (_tenantContext.BypassTenantFilters) return;

        var currentTenantId = _tenantContext.TenantId;
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (currentTenantId == Guid.Empty)
        {
            foreach (var entry in entries)
            {
                var tenantProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == TenantIdPropertyName);
                if (tenantProperty is null || tenantProperty.Metadata.ClrType != typeof(Guid)) continue;

                var value = (Guid)(tenantProperty.CurrentValue ?? Guid.Empty);
                if (value == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Cannot persist {entry.Metadata.ClrType.Name} without an explicit TenantId when no tenant context is resolved.");
                }
            }

            return;
        }

        // Development anónimo usa un DefaultTenantId sintético; si la entidad ya trae TenantId
        // explícito (login/bootstrap), permitir la escritura sin forzar el default.
        var isSyntheticDevTenant = currentTenantId == Guid.Parse("00000000-0000-0000-0000-000000000001");
        if (isSyntheticDevTenant)
        {
            var allHaveExplicitTenant = entries.All(entry =>
            {
                var tenantProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == TenantIdPropertyName);
                if (tenantProperty is null || tenantProperty.Metadata.ClrType != typeof(Guid)) return true;
                var value = (Guid)(tenantProperty.CurrentValue ?? Guid.Empty);
                return value != Guid.Empty;
            });

            if (allHaveExplicitTenant)
            {
                return;
            }
        }

        foreach (var entry in entries)
        {
            var tenantProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == TenantIdPropertyName);
            if (tenantProperty is null) continue;
            if (tenantProperty.Metadata.ClrType != typeof(Guid)) continue;

            if (entry.State == EntityState.Added)
            {
                var value = (Guid)(tenantProperty.CurrentValue ?? Guid.Empty);
                if (value == Guid.Empty)
                {
                    tenantProperty.CurrentValue = currentTenantId;
                }
                else if (value != currentTenantId)
                {
                    throw new InvalidOperationException(
                        $"Cross-tenant insert rejected for {entry.Metadata.ClrType.Name}: entity TenantId {value} does not match current tenant {currentTenantId}.");
                }

                continue;
            }

            var current = (Guid)(tenantProperty.CurrentValue ?? Guid.Empty);
            var original = tenantProperty.OriginalValue is Guid originalGuid ? originalGuid : current;

            if (entry.State == EntityState.Modified && tenantProperty.IsModified && current != original)
            {
                throw new InvalidOperationException(
                    $"TenantId is immutable and cannot be changed on {entry.Metadata.ClrType.Name}.");
            }

            var effectiveTenantId = entry.State == EntityState.Modified ? original : current;
            if (effectiveTenantId != currentTenantId)
            {
                throw new InvalidOperationException(
                    $"Cross-tenant access rejected for {entry.Metadata.ClrType.Name}: entity TenantId {effectiveTenantId} does not match current tenant {currentTenantId}.");
            }
        }
    }
}
