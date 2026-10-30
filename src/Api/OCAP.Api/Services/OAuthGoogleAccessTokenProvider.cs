using Microsoft.Extensions.Options;
using OCAP.Providers.Google.Abstractions;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Services;

/// <summary>
/// Token para Google Workspace: el de la conexión OAuth de la organización (se guarda al pulsar "Conectar Google")
/// y, si no existe, <c>Google:AccessToken</c> del entorno. La conexión es de la organización, no de una persona
/// (como la modela IntegrationsController): el agente la usa también en conversaciones de canales externos.
/// </summary>
public sealed class OAuthGoogleAccessTokenProvider : IGoogleAccessTokenProvider
{
    private readonly GoogleOAuthConnectionStore _connections;
    private readonly ITenantContext _tenant;
    private readonly IOptionsMonitor<GoogleWorkspaceOptions> _options;

    public OAuthGoogleAccessTokenProvider(
        GoogleOAuthConnectionStore connections,
        ITenantContext tenant,
        IOptionsMonitor<GoogleWorkspaceOptions> options)
    {
        _connections = connections;
        _tenant = tenant;
        _options = options;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Falla cerrado: sin una organización concreta nunca se toma el token de otra.
        if (_tenant.TenantId == Guid.Empty || _tenant.BypassTenantFilters)
            return NullIfEmpty(_options.CurrentValue.AccessToken);

        return await _connections.GetAccessTokenAsync(_tenant.TenantId, cancellationToken)
            ?? NullIfEmpty(_options.CurrentValue.AccessToken);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
