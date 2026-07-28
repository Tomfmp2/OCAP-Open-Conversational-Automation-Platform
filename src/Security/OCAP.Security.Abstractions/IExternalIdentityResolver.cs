namespace OCAP.Security.Abstractions;

// Contrato de seguridad para la resolución y vinculación de identidades de usuarios entre canales externos y OCAP.
public interface IExternalIdentityResolver
{
    // Resuelve el UserId interno de OCAP asociado a una identidad de proveedor externo (Telegram, WhatsApp, Slack, etc.) dentro de un Tenant.
    Task<Guid?> ResolveUserIdAsync(Guid tenantId, string provider, string externalId, CancellationToken cancellationToken = default);

    // Vincula una nueva identidad externa a un UserId interno preexistente dentro de un Tenant.
    Task<bool> LinkExternalIdentityAsync(Guid tenantId, Guid userId, string provider, string externalId, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
}
