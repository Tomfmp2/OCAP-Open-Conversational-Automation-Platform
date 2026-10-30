using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Services;

/// <summary>Secciones de Tenants.SettingsJson (correo, Google, preferencias): leer y reemplazar una sin tocar las demás.</summary>
public static class TenantSettingsSections
{
    public static async Task<T?> ReadAsync<T>(OCAPDbContext db, Guid tenantId, string section, CancellationToken cancellationToken) where T : class
    {
        var json = await db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).Select(t => t.SettingsJson).FirstOrDefaultAsync(cancellationToken);
        return json is not null && JsonNode.Parse(json) is JsonObject root && root[section] is { } node ? node.Deserialize<T>() : null;
    }

    /// <summary>Guarda la sección; null la elimina.</summary>
    public static async Task WriteAsync<T>(OCAPDbContext db, Guid tenantId, string section, T? value, CancellationToken cancellationToken) where T : class
    {
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == tenantId, cancellationToken);
        var root = JsonNode.Parse(tenant.SettingsJson) as JsonObject ?? new JsonObject();
        if (value is null) root.Remove(section);
        else root[section] = JsonSerializer.SerializeToNode(value);
        tenant.UpdateSettings(root.ToJsonString());
        await db.SaveChangesAsync(cancellationToken);
    }

    public static bool HasSection(string settingsJson, string section) =>
        JsonNode.Parse(settingsJson) is JsonObject root && root[section] is not null;
}
