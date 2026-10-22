using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OCAP.Infrastructure.Persistence.Context;
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
    private readonly OCAPDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOptionsMonitor<GoogleWorkspaceOptions> _options;

    public OAuthGoogleAccessTokenProvider(
        OCAPDbContext db,
        ITenantContext tenant,
        IOptionsMonitor<GoogleWorkspaceOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _options = options;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Falla cerrado: sin una organización concreta nunca se toma el token de otra.
        if (_tenant.TenantId == Guid.Empty || _tenant.BypassTenantFilters)
            return NullIfEmpty(_options.CurrentValue.AccessToken);

        var tenantId = _tenant.TenantId;
        var connected = await _db.OAuthConnections.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Provider.ToLower() == "google" && c.AccessToken != "")
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => c.AccessToken)
            .FirstOrDefaultAsync(cancellationToken);

        return connected ?? NullIfEmpty(_options.CurrentValue.AccessToken);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
