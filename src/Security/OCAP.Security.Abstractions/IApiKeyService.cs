using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato para la generación, hash, validación, revocación y gestión de claves de API (X-API-Key).
public interface IApiKeyService
{
    // Genera una nueva clave de API retornando el secreto en texto plano (una sola vez) y la entidad hash para DB.
    (string RawApiKey, ApiKey ApiKeyEntity) CreateApiKey(Guid tenantId, Guid userId, string name, TimeSpan validFor);

    // Genera una nueva clave de API con scopes específicos.
    (string RawApiKey, ApiKey ApiKeyEntity) CreateApiKey(Guid tenantId, Guid userId, string name, IEnumerable<string> scopes, TimeSpan validFor);

    // Valida una clave de API entrante (X-API-Key) comparando su hash y verificando scopes requeridos.
    Task<ApiKey?> ValidateApiKeyAsync(string rawApiKey, string? requiredScope = null, CancellationToken cancellationToken = default);

    // Revoca una clave de API por su ID.
    Task<bool> RevokeApiKeyAsync(Guid apiKeyId, Guid tenantId, CancellationToken cancellationToken = default);

    // Obtiene todas las API Keys activas para un tenant.
    Task<IReadOnlyList<ApiKey>> GetApiKeysForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
