using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Team;

/// <summary>Lo que el panel ve del SMTP de la organización: nunca la contraseña, solo si hay una guardada.</summary>
public sealed record OrganizationEmailSettingsDto(string? Host, int Port, string? Username, string? From, bool UseSsl, bool HasPassword);

/// <summary>
/// SMTP propio de cada organización, en Tenants.SettingsJson ("Smtp"). La contraseña va cifrada en el vault; en el
/// JSON queda solo su referencia.
/// </summary>
public sealed class OrganizationEmailSettings
{
    private const string Section = "Smtp";
    private const string PasswordSecretKey = "smtp-password";

    /// <summary>Puertos de correo: limitarlos impide usar el SMTP para tocar otros servicios.</summary>
    public static readonly IReadOnlySet<int> AllowedPorts = new HashSet<int> { 25, 465, 587, 2525 };

    private readonly OCAPDbContext _db;
    private readonly ICredentialVault _vault;

    public OrganizationEmailSettings(OCAPDbContext db, ICredentialVault vault)
    {
        _db = db;
        _vault = vault;
    }

    public async Task<SmtpOptions?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var stored = await ReadAsync(tenantId, cancellationToken);
        if (stored is null) return null;
        return new SmtpOptions
        {
            Host = stored.Host,
            Port = stored.Port,
            Username = stored.Username,
            From = stored.From,
            UseSsl = stored.UseSsl,
            Password = stored.PasswordRef is null ? null : await _vault.RetrieveSecretAsync(tenantId, stored.PasswordRef, cancellationToken)
        };
    }

    public async Task<OrganizationEmailSettingsDto?> DescribeAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await ReadAsync(tenantId, cancellationToken) is { } s ? new(s.Host, s.Port, s.Username, s.From, s.UseSsl, s.PasswordRef is not null) : null;

    // SettingsJson es jsonb: se revisa en memoria (hay pocas organizaciones por instalación).
    public async Task<bool> AnyConfiguredAsync(CancellationToken cancellationToken) =>
        (await _db.Tenants.IgnoreQueryFilters().Where(t => t.IsActive).Select(t => t.SettingsJson).ToListAsync(cancellationToken))
            .Any(json => JsonNode.Parse(json) is JsonObject root && root[Section] is not null);

    /// <summary>Guarda el SMTP. La contraseña: null conserva la guardada; vacía la borra.</summary>
    public async Task SaveAsync(Guid tenantId, OrganizationEmailSettingsDto settings, string? password, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == tenantId, cancellationToken);
        var root = JsonNode.Parse(tenant.SettingsJson) as JsonObject ?? new JsonObject();
        var previous = root[Section]?.Deserialize<Stored>();

        var passwordRef = previous?.PasswordRef;
        if (password is not null)
        {
            if (passwordRef is not null) await _vault.DeleteSecretAsync(tenantId, passwordRef, cancellationToken);
            passwordRef = password.Length == 0 ? null : await _vault.StoreSecretAsync(tenantId, PasswordSecretKey, password, cancellationToken);
        }

        root[Section] = JsonSerializer.SerializeToNode(new Stored(
            settings.Host?.Trim(), settings.Port, settings.Username?.Trim(), settings.From?.Trim(), settings.UseSsl, passwordRef));
        tenant.UpdateSettings(root.ToJsonString());
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == tenantId, cancellationToken);
        if (JsonNode.Parse(tenant.SettingsJson) is not JsonObject root || root[Section] is null) return;
        if (root[Section]!.Deserialize<Stored>()?.PasswordRef is { } reference)
            await _vault.DeleteSecretAsync(tenantId, reference, cancellationToken);
        root.Remove(Section);
        tenant.UpdateSettings(root.ToJsonString());
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// El servidor SMTP lo escribe un usuario: solo destinos públicos, para que el panel no sirva para tocar puertos
    /// de la red interna. Un relé interno se configura en el servidor (Email:Smtp).
    /// </summary>
    public static async Task<string?> ValidateHostAsync(string host, CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        try
        {
            addresses = IPAddress.TryParse(host, out var literal) ? [literal] : await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (SocketException)
        {
            return $"No se pudo resolver el servidor '{host}'.";
        }
        if (addresses.Length == 0) return $"No se pudo resolver el servidor '{host}'.";
        return addresses.Any(a => OutboundUrlGuard.IsBlocked(a, allowPrivateNetwork: false))
            ? $"El servidor '{host}' está en una red interna. Configúralo en el servidor de OCAP (Email:Smtp)."
            : null;
    }

    private async Task<Stored?> ReadAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var json = await _db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).Select(t => t.SettingsJson).FirstOrDefaultAsync(cancellationToken);
        return json is not null && JsonNode.Parse(json) is JsonObject root && root[Section] is { } node ? node.Deserialize<Stored>() : null;
    }

    private sealed record Stored(string? Host, int Port, string? Username, string? From, bool UseSsl, string? PasswordRef);
}
