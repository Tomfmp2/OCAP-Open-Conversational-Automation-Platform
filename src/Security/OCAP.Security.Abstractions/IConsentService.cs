using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Interfaz para el servicio de gestión de consentimiento de usuarios OAuth2/OIDC (CAP-14).
public interface IConsentService
{
    Task<UserConsent> GrantConsentAsync(Guid tenantId, Guid userId, string clientId, IEnumerable<string> scopes, CancellationToken cancellationToken = default);
    Task<UserConsent?> GetConsentAsync(Guid tenantId, Guid userId, string clientId, CancellationToken cancellationToken = default);
    Task<bool> HasConsentAsync(Guid tenantId, Guid userId, string clientId, IEnumerable<string> scopes, CancellationToken cancellationToken = default);
    Task<bool> RevokeConsentAsync(Guid consentId, Guid tenantId, CancellationToken cancellationToken = default);
}
