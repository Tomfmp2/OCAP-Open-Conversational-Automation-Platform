using Microsoft.Extensions.Logging;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de auditoría que guarda y registra la bitácora de seguridad de OCAP.
public class SecurityAuditService : ISecurityAuditService
{
    private readonly List<AuditLog> _auditLogs = new();
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task LogSecurityEventAsync(Guid tenantId, Guid userId, string action, string details, string ipAddress, bool success, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog(Guid.NewGuid(), tenantId, userId, action, details, ipAddress, success);
        _auditLogs.Add(log);

        _logger.LogInformation("Auditoría Seguridad: Tenant {TenantId}, Usuario {UserId}, Acción {Action}, IP {IpAddress}, Resultado {Success}",
            tenantId, userId, action, ipAddress, success);

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<AuditLog> GetLogs() => _auditLogs.AsReadOnly();
}
