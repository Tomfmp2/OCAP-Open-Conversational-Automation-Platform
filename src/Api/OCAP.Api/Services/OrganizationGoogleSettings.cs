using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Services;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Services;

/// <summary>
/// Cliente OAuth de Google de la organización (Google Cloud Console), en Tenants.SettingsJson ("Google"). El client
/// secret va cifrado en el vault.
/// </summary>
public sealed class OrganizationGoogleSettings
{
    private const string Section = "Google";

    private readonly OCAPDbContext _db;
    private readonly ICredentialVault _vault;

    public OrganizationGoogleSettings(OCAPDbContext db, ICredentialVault vault)
    {
        _db = db;
        _vault = vault;
    }

    public async Task<(string ClientId, string ClientSecret)?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var stored = await TenantSettingsSections.ReadAsync<Stored>(_db, tenantId, Section, cancellationToken);
        if (stored is null) return null;
        var secret = await _vault.RetrieveSecretAsync(tenantId, stored.ClientSecretRef, cancellationToken);
        return string.IsNullOrWhiteSpace(secret) ? null : (stored.ClientId, secret);
    }

    public async Task<string?> GetClientIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
        (await TenantSettingsSections.ReadAsync<Stored>(_db, tenantId, Section, cancellationToken))?.ClientId;

    /// <param name="tenantId">Organización.</param>
    /// <param name="clientId">Client ID de Google.</param>
    /// <param name="clientSecret">Null conserva el guardado.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    public async Task SaveAsync(Guid tenantId, string clientId, string? clientSecret, CancellationToken cancellationToken)
    {
        var previous = await TenantSettingsSections.ReadAsync<Stored>(_db, tenantId, Section, cancellationToken);
        var reference = previous?.ClientSecretRef;
        if (clientSecret is not null)
        {
            if (reference is not null) await _vault.DeleteSecretAsync(tenantId, reference, cancellationToken);
            reference = await _vault.StoreSecretAsync(tenantId, "google-client-secret", clientSecret, cancellationToken);
        }
        if (reference is null) throw new ArgumentException("Falta el client secret de Google.");
        await TenantSettingsSections.WriteAsync(_db, tenantId, Section, new Stored(clientId.Trim(), reference), cancellationToken);
    }

    public async Task ClearAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await TenantSettingsSections.ReadAsync<Stored>(_db, tenantId, Section, cancellationToken) is not { } stored) return;
        await _vault.DeleteSecretAsync(tenantId, stored.ClientSecretRef, cancellationToken);
        await TenantSettingsSections.WriteAsync<Stored>(_db, tenantId, Section, null, cancellationToken);
    }

    private sealed record Stored(string ClientId, string ClientSecretRef);
}
