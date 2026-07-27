using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Application.UseCases;

// DTO con el resultado de creación de clave de API.
public record CreateApiKeyResult(
    string RawApiKey,
    Guid ApiKeyId,
    string Prefix,
    string Name,
    DateTime ExpiresAtUtc
);

// Caso de uso para emitir claves de API seguras (X-API-Key).
public class CreateApiKeyUseCase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ISecurityAuditService _auditService;

    public CreateApiKeyUseCase(IApiKeyService apiKeyService, ISecurityAuditService auditService)
    {
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<CreateApiKeyResult> ExecuteAsync(Guid tenantId, Guid userId, string name, TimeSpan validFor, string ipAddress, CancellationToken cancellationToken = default)
    {
        var (rawKey, entity) = _apiKeyService.CreateApiKey(tenantId, userId, name, validFor);
        await _auditService.LogSecurityEventAsync(tenantId, userId, "ApiKey.Create", $"Creación de API Key: {name}", ipAddress, true, cancellationToken);
        return new CreateApiKeyResult(rawKey, entity.Id, entity.Prefix, entity.Name, entity.ExpiresAtUtc);
    }
}
