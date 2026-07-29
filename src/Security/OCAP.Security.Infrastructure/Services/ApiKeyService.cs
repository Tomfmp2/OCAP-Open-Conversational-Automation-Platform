using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de claves de API (X-API-Key) respaldado por persistencia EF Core PostgreSQL / In-Memory.
public class ApiKeyService : IApiKeyService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ILogger<ApiKeyService>? _logger;

    public ApiKeyService(OCAPDbContext dbContext, ILogger<ApiKeyService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public (string RawApiKey, ApiKey ApiKeyEntity) CreateApiKey(Guid tenantId, Guid userId, string name, TimeSpan validFor)
    {
        return CreateApiKey(tenantId, userId, name, new[] { "*" }, validFor);
    }

    public (string RawApiKey, ApiKey ApiKeyEntity) CreateApiKey(Guid tenantId, Guid userId, string name, IEnumerable<string> scopes, TimeSpan validFor)
    {
        var prefix = "ocap_live_";
        var secretPart = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "").Replace("/", "").Replace("=", "");

        var rawApiKey = $"{prefix}{secretPart}";
        var hash = ComputeHash(rawApiKey);
        var scopesJoined = string.Join(",", scopes ?? Array.Empty<string>());

        var apiKeyEntity = new ApiKey(
            Guid.NewGuid(),
            tenantId,
            userId,
            hash,
            prefix,
            name,
            scopesJoined,
            DateTime.UtcNow.Add(validFor)
        );

        _dbContext.ApiKeys.Add(apiKeyEntity);
        _dbContext.SaveChanges();

        _logger?.LogInformation("API Key creada para Tenant {TenantId}, Prefix {Prefix}**** (ID: {Id})",
            tenantId, prefix, apiKeyEntity.Id);

        return (rawApiKey, apiKeyEntity);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string rawApiKey, string? requiredScope = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey)) return null;

        var hash = ComputeHash(rawApiKey);
        var keyEntity = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == hash && !k.IsRevoked && k.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

        if (keyEntity == null)
        {
            _logger?.LogWarning("Intento de validación fallido para API Key (Hash no encontrado o revocada/expirada)");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(requiredScope) && !keyEntity.HasScope(requiredScope))
        {
            _logger?.LogWarning("API Key {Id} no cuenta con el scope requerido: {RequiredScope}", keyEntity.Id, requiredScope);
            return null;
        }

        keyEntity.RecordUsage();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return keyEntity;
    }

    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var key = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.TenantId == tenantId, cancellationToken);
        if (key == null) return false;

        key.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation("API Key {Id} fue revocada para Tenant {TenantId}", apiKeyId, tenantId);
        return true;
    }

    public async Task<IReadOnlyList<ApiKey>> GetApiKeysForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var keys = await _dbContext.ApiKeys
            .Where(k => k.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        return keys;
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
