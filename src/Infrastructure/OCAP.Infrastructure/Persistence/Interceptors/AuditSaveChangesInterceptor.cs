using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OCAP.Security.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace OCAP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Audit trail automático. Fail-safe: nunca inventa un TenantId que no exista en el cambio.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var claimTenantId = ResolveClaimTenantId();
        var userId = ResolveUserId();
        var pendingTenantIds = entries
            .Where(e => e.State == EntityState.Added && e.Entity is Tenant)
            .Select(e => ((Tenant)e.Entity).Id)
            .ToHashSet();

        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.Entity is Tenant) continue;
            if (entry.Entity.GetType().Name is "OutboxMessage") continue;

            var entityTenantId = TryGetEntityTenantId(entry);
            Guid? tenantId = entityTenantId is Guid et && et != Guid.Empty
                ? et
                : null;

            if (tenantId is null && claimTenantId != Guid.Empty)
            {
                tenantId = claimTenantId;
            }

            if (tenantId is null && pendingTenantIds.Count > 0)
            {
                tenantId = pendingTenantIds.First();
            }

            if (tenantId is null || tenantId == Guid.Empty)
            {
                continue;
            }

            var action = entry.State.ToString();
            var entityName = entry.Entity.GetType().Name;
            var details = JsonSerializer.Serialize(new { Entity = entityName, Action = action });
            auditLogs.Add(new AuditLog(
                Guid.NewGuid(),
                tenantId.Value,
                userId,
                $"Entity_{action}_{entityName}",
                details,
                GetClientIp(),
                true));
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static Guid? TryGetEntityTenantId(EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
        if (prop?.CurrentValue is Guid tenantId && tenantId != Guid.Empty)
        {
            return tenantId;
        }

        return null;
    }

    private Guid ResolveClaimTenantId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return Guid.Empty;

        var tenantClaim = user.FindFirst("tenant_id")?.Value
                          ?? user.FindFirst("TenantId")?.Value;

        return !string.IsNullOrWhiteSpace(tenantClaim) && Guid.TryParse(tenantClaim, out var parsed)
            ? parsed
            : Guid.Empty;
    }

    private Guid ResolveUserId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user == null) return Guid.Empty;

        var userClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value
                        ?? user.FindFirst("user_id")?.Value;

        return !string.IsNullOrWhiteSpace(userClaim) && Guid.TryParse(userClaim, out var parsed)
            ? parsed
            : Guid.Empty;
    }

    private string GetClientIp()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.Request is null) return "system";

        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor)
            && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return httpContext.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
