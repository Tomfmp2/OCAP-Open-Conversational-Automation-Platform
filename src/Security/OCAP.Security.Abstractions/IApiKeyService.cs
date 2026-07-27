using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato para la generación, hash, validación y rotación de claves de API (X-API-Key).
public interface IApiKeyService
{
    // Genera una nueva clave de API retornando el secreto en texto plano (una sola vez) y la entidad hash para DB.
    (string RawApiKey, ApiKey ApiKeyEntity) CreateApiKey(Guid tenantId, Guid userId, string name, TimeSpan validFor);

    // Valida una clave de API entrante (X-API-Key) comparando su hash.
    Task<ApiKey?> ValidateApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default);
}
