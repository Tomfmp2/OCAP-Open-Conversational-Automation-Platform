using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato de seguridad para la resolución, vinculación y desvinculación de identidades de usuarios entre proveedores externos y OCAP (CAP-15).
public interface IExternalIdentityResolver
{
    // Resuelve el UserId interno de OCAP asociado a una identidad de proveedor externo (Google, Microsoft, GitHub, OIDC, Telegram, WhatsApp, etc.) dentro de un Tenant.
    Task<Guid?> ResolveUserIdAsync(Guid tenantId, string provider, string externalId, CancellationToken cancellationToken = default);

    // Vincula una nueva identidad externa a un UserId interno preexistente dentro de un Tenant.
    Task<bool> LinkExternalIdentityAsync(Guid tenantId, Guid userId, string provider, string externalId, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    // Desvincula un proveedor externo de un usuario dentro de un Tenant (CAP-15).
    Task<bool> UnlinkExternalIdentityAsync(Guid tenantId, Guid userId, string provider, CancellationToken cancellationToken = default);

    // Obtiene todas las identidades externas vinculadas a un usuario en un Tenant (CAP-15).
    Task<IReadOnlyList<ExternalIdentity>> GetLinkedIdentitiesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
