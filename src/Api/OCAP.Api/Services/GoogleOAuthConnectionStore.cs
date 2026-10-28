using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Services;

/// <summary>
/// Conexión OAuth de Google de cada organización. Los tokens se guardan cifrados con el vault (AES por organización):
/// una copia de la base no da acceso a Gmail, Calendar ni Sheets. El access token dura una hora y se renueva aquí con el
/// refresh token antes de vencer.
/// </summary>
public sealed class GoogleOAuthConnectionStore
{
    public const string Provider = "Google";
    private const string AccessTokenSecret = "google-oauth-access-token";
    private const string RefreshTokenSecret = "google-oauth-refresh-token";
    private const string VaultPrefix = "vault:";
    private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(2);

    private readonly OCAPDbContext _db;
    private readonly ICredentialVault _vault;
    private readonly GoogleOAuthClient _client;
    private readonly ILogger<GoogleOAuthConnectionStore> _logger;

    public GoogleOAuthConnectionStore(
        OCAPDbContext db, ICredentialVault vault, GoogleOAuthClient client, ILogger<GoogleOAuthConnectionStore> logger)
    {
        _db = db;
        _vault = vault;
        _client = client;
        _logger = logger;
    }

    public async Task SaveAsync(Guid tenantId, Guid userId, GoogleTokenGrant grant, string scopes, CancellationToken cancellationToken)
    {
        var accessToken = await _vault.StoreSecretAsync(tenantId, AccessTokenSecret, grant.AccessToken, cancellationToken);
        // Google solo envía refresh_token la primera vez: si no llega, se conserva el que ya estaba.
        var refreshToken = string.IsNullOrWhiteSpace(grant.RefreshToken)
            ? string.Empty
            : await _vault.StoreSecretAsync(tenantId, RefreshTokenSecret, grant.RefreshToken, cancellationToken);

        var existing = await FindAsync(tenantId, cancellationToken);
        if (existing is null)
        {
            _db.OAuthConnections.Add(new OAuthConnection(
                Guid.NewGuid(), userId, Provider, accessToken, refreshToken, grant.ExpiresAtUtc, scopes, tenantId));
        }
        else
        {
            existing.UpdateTokens(accessToken, refreshToken, grant.ExpiresAtUtc);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Access token vigente de la organización, renovado si está por vencer; null si no hay conexión usable.</summary>
    public async Task<string?> GetAccessTokenAsync(Guid tenantId, CancellationToken cancellationToken, bool forceRefresh = false)
    {
        var connection = await FindAsync(tenantId, cancellationToken);
        if (connection is null || string.IsNullOrWhiteSpace(connection.AccessToken)) return null;

        var accessToken = await RevealAsync(tenantId, connection.AccessToken, cancellationToken);
        if (!forceRefresh && connection.TokenExpiration > DateTime.UtcNow.Add(RenewBefore))
        {
            if (!IsEncrypted(connection.AccessToken) && accessToken is not null)
            {
                // Conexión guardada antes del cifrado: se protege en el primer uso.
                await ReencryptAsync(tenantId, connection, accessToken, cancellationToken);
            }
            return accessToken;
        }

        var refreshToken = await RevealAsync(tenantId, connection.RefreshToken, cancellationToken);
        if (refreshToken is null || !_client.IsConfigured)
        {
            return connection.TokenExpiration > DateTime.UtcNow ? accessToken : null;
        }

        try
        {
            var grant = await _client.RefreshAsync(refreshToken, cancellationToken);
            await SaveAsync(tenantId, connection.UserId, grant with { RefreshToken = grant.RefreshToken ?? refreshToken },
                connection.Scopes, cancellationToken);
            return grant.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo renovar el token de Google de la organización {TenantId}.", tenantId);
            return connection.TokenExpiration > DateTime.UtcNow ? accessToken : null;
        }
    }

    private Task<OAuthConnection?> FindAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _db.OAuthConnections
            .Where(c => c.TenantId == tenantId && c.Provider.ToLower() == "google")
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task ReencryptAsync(Guid tenantId, OAuthConnection connection, string accessToken, CancellationToken cancellationToken)
    {
        var refreshToken = await RevealAsync(tenantId, connection.RefreshToken, cancellationToken);
        await SaveAsync(tenantId, connection.UserId,
            new GoogleTokenGrant(accessToken, refreshToken, connection.TokenExpiration, null), connection.Scopes, cancellationToken);
    }

    // Acepta también el texto plano de conexiones anteriores al cifrado.
    private async Task<string?> RevealAsync(Guid tenantId, string stored, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        return IsEncrypted(stored) ? await _vault.RetrieveSecretAsync(tenantId, stored, cancellationToken) : stored;
    }

    private static bool IsEncrypted(string stored) => stored.StartsWith(VaultPrefix, StringComparison.Ordinal);
}
