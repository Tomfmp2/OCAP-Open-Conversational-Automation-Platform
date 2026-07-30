using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Enforce tenant isolation on write paths: assign TenantId on insert,
/// reject cross-tenant writes, and block TenantId mutations.
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
        if (currentTenantId == Guid.Empty)
            throw new InvalidOperationException("Cannot persist tenant-scoped data without a resolved TenantId.");

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

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
