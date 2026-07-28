using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OCAP.Security.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace OCAP.Infrastructure.Persistence.Interceptors;

// Interceptor para implementar el Audit Trail automáticamente en el DbContext
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
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        if (!entries.Any()) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditLogs = new List<AuditLog>();
        var (tenantId, userId) = ResolveTenantAndUser();

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog) continue; // No auditar la auditoría
            if (entry.Entity.GetType().Name == "OutboxMessage") continue; // No auditar el outbox

            var action = entry.State.ToString();
            var entityName = entry.Entity.GetType().Name;
            
            var details = JsonSerializer.Serialize(new
            {
                Entity = entityName,
                Action = action
            });

            var log = new AuditLog(Guid.NewGuid(), tenantId, userId, $"Entity_{action}_{entityName}", details, GetClientIp(), true);
            auditLogs.Add(log);
        }

        if (auditLogs.Any())
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private (Guid tenantId, Guid userId) ResolveTenantAndUser()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User == null)
        {
            return (Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Empty);
        }

        var user = httpContext.User;

        // Tenant ID
        var tenantClaim = user.FindFirst("tenant_id")?.Value
                          ?? user.FindFirst("TenantId")?.Value
                          ?? user.FindFirst(ClaimTypes.GroupSid)?.Value;

        var tenantId = (!string.IsNullOrWhiteSpace(tenantClaim) && Guid.TryParse(tenantClaim, out var parsedTenantId))
            ? parsedTenantId
            : Guid.Parse("00000000-0000-0000-0000-000000000001");

        // User ID
        var userClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value
                        ?? user.FindFirst("user_id")?.Value;

        var userId = (!string.IsNullOrWhiteSpace(userClaim) && Guid.TryParse(userClaim, out var parsedUserId))
            ? parsedUserId
            : Guid.Empty;

        return (tenantId, userId);
    }

    private string GetClientIp()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null) return "127.0.0.1";

        if (httpContext.Request?.Headers != null && httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return httpContext.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}

