using OCAP.Security.Abstractions;

namespace OCAP.Security.Application.UseCases;

// Caso de uso para revocar claves de API existentes.
public class RevokeApiKeyUseCase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ISecurityAuditService _auditService;

    public RevokeApiKeyUseCase(IApiKeyService apiKeyService, ISecurityAuditService auditService)
    {
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<bool> ExecuteAsync(Guid apiKeyId, Guid tenantId, Guid userId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var success = await _apiKeyService.RevokeApiKeyAsync(apiKeyId, tenantId, cancellationToken);
        if (success)
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "ApiKey.Revoke", $"Revocación de API Key ID: {apiKeyId}", ipAddress, true, cancellationToken);
        }
        return success;
    }
}
