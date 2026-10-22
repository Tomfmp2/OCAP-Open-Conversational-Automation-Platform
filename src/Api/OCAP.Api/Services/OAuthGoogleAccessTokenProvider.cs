using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Providers.Google.Abstractions;

namespace OCAP.Api.Services;

/// <summary>
/// Token para Google Workspace: el de la conexión OAuth del tenant (se guarda al pulsar "Conectar Google")
/// y, si no existe, <c>Google:AccessToken</c> del entorno. El filtro de tenant del DbContext aísla las cuentas.
/// </summary>
public sealed class OAuthGoogleAccessTokenProvider : IGoogleAccessTokenProvider
{
    private readonly OCAPDbContext _db;
    private readonly IOptionsMonitor<GoogleWorkspaceOptions> _options;

    public OAuthGoogleAccessTokenProvider(OCAPDbContext db, IOptionsMonitor<GoogleWorkspaceOptions> options)
    {
        _db = db;
        _options = options;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var connected = await _db.OAuthConnections.AsNoTracking()
            .Where(c => c.Provider.ToLower() == "google" && c.AccessToken != "")
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => c.AccessToken)
            .FirstOrDefaultAsync(cancellationToken);

        return connected ?? NullIfEmpty(_options.CurrentValue.AccessToken);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
