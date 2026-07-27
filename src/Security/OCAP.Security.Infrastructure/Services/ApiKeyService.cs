using System.Security.Cryptography;
using System.Text;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de claves de API (X-API-Key) con almacenamiento seguro en SHA256.
public class ApiKeyService : IApiKeyService
{
    private readonly List<ApiKey> _store = new(); // In-memory store para validación previa a DB

    public (string RawApiKey, ApiKey ApiKeyEntity) CreateApiKey(Guid tenantId, Guid userId, string name, TimeSpan validFor)
    {
        var prefix = "ocap_live_";
        var secretPart = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "").Replace("/", "").Replace("=", "");

        var rawApiKey = $"{prefix}{secretPart}";
        var hash = ComputeHash(rawApiKey);

        var apiKeyEntity = new ApiKey(
            Guid.NewGuid(),
            tenantId,
            userId,
            hash,
            prefix,
            name,
            DateTime.UtcNow.Add(validFor)
        );

        _store.Add(apiKeyEntity);
        return (rawApiKey, apiKeyEntity);
    }

    public Task<ApiKey?> ValidateApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey)) return Task.FromResult<ApiKey?>(null);

        var hash = ComputeHash(rawApiKey);
        var keyEntity = _store.FirstOrDefault(k => k.KeyHash == hash && k.IsActive);

        if (keyEntity != null)
        {
            keyEntity.RecordUsage();
        }

        return Task.FromResult(keyEntity);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
