namespace OCAP.Security.Abstractions;

// Contrato para la auditoría de eventos de seguridad (Login, Logout, API Keys, Roles).
public interface ISecurityAuditService
{
    // Registra un evento de seguridad en la bitácora de auditoría.
    Task LogSecurityEventAsync(Guid tenantId, Guid userId, string action, string details, string ipAddress, bool success, CancellationToken cancellationToken = default);
}
