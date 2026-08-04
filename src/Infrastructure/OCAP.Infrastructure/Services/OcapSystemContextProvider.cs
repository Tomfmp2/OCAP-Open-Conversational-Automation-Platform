using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Services;

/// <summary>
/// Lee el estado real del tenant (canales, agentes, IA, conocimiento, usuarios)
/// para que el agente madre responda con datos, no con guías genéricas.
/// </summary>
public sealed class OcapSystemContextProvider : IOcapSystemContextProvider
{
    private readonly OCAPDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcapSystemContextProvider> _logger;

    public OcapSystemContextProvider(
        OCAPDbContext db,
        IConfiguration configuration,
        ILogger<OcapSystemContextProvider> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ESTADO REAL DEL TENANT (fuente de verdad; úsalo para responder) ===");
            sb.AppendLine($"TenantId: {tenantId}");
            sb.AppendLine($"GeneradoUtc: {DateTime.UtcNow:O}");
            sb.AppendLine();

            // Canales
            var channels = await _db.ChannelConnections
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .OrderBy(c => c.Provider)
                .ThenBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);

            sb.AppendLine($"CANALES ({channels.Count} total, {channels.Count(c => c.Enabled)} activos):");
            if (channels.Count == 0)
            {
                sb.AppendLine("- (ninguno registrado)");
            }
            else
            {
                foreach (var c in channels)
                {
                    var account = c.ConfigurationMetadata.TryGetValue("AccountIdentifier", out var a) ? a
                        : c.ConfigurationMetadata.TryGetValue("Username", out var u) ? u
                        : c.ConfigurationMetadata.TryGetValue("PhoneNumberId", out var p) ? p
                        : c.ConfigurationMetadata.TryGetValue("InstanceName", out var i) ? i
                        : "—";
                    var mode = c.ConfigurationMetadata.TryGetValue("ConnectionMode", out var m) ? m : "—";
                    sb.AppendLine(
                        $"- Id={c.Id} | Provider={c.Provider} | Nombre=\"{c.DisplayName}\" | Enabled={c.Enabled} | Cuenta/Ref={account} | Modo={mode} | Creado={c.CreatedAtUtc:u}");
                }
            }

            sb.AppendLine();

            // Agentes
            var agents = await _db.Agents.AsNoTracking()
                .Where(a => a.TenantId == tenantId || a.TenantId == Guid.Empty)
                .ToListAsync(cancellationToken);
            sb.AppendLine($"AGENTES ({agents.Count}):");
            if (agents.Count == 0)
            {
                sb.AppendLine("- (ninguno en catálogo; el Agente Principal/madre siempre está disponible en el chat de inicio)");
            }
            else
            {
                foreach (var ag in agents.Take(30))
                {
                    sb.AppendLine($"- Id={ag.Id} | Name={ag.Name.Value} | Status={ag.Status} | Desc={Truncate(ag.Description, 80)}");
                }
            }

            sb.AppendLine();

            // Proveedores IA configurados en DB
            var aiConfigs = await _db.AiProviderConfigurations
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            var envGemini = !string.IsNullOrWhiteSpace(_configuration["AiProviders:Gemini:ApiKey"]);
            var envOpenAi = !string.IsNullOrWhiteSpace(_configuration["AiProviders:OpenAI:ApiKey"]);
            var preferred = _configuration["AiProviders:PreferredProvider"] ?? "Gemini";
            var geminiModel = _configuration["AiProviders:Gemini:ModelName"] ?? "gemini-3.5-flash";

            sb.AppendLine("IA:");
            sb.AppendLine($"- PreferredProvider (env)={preferred}");
            sb.AppendLine($"- Gemini env key={(envGemini ? "sí" : "no")} model={geminiModel}");
            sb.AppendLine($"- OpenAI env key={(envOpenAi ? "sí" : "no")}");
            sb.AppendLine($"- Configs en DB del tenant: {aiConfigs.Count}");
            foreach (var cfg in aiConfigs)
            {
                sb.AppendLine(
                    $"- DB: {cfg.ProviderName} | Model={cfg.ModelName} | Enabled={cfg.IsEnabled} | Display={cfg.DisplayName}");
            }

            sb.AppendLine();

            // Conocimiento
            var kbs = await _db.KnowledgeBases
                .AsNoTracking()
                .Where(k => k.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            sb.AppendLine($"CONOCIMIENTO ({kbs.Count} bases):");
            if (kbs.Count == 0)
            {
                sb.AppendLine("- (ninguna base)");
            }
            else
            {
                foreach (var kb in kbs.Take(20))
                {
                    var docs = await _db.KnowledgeDocuments.AsNoTracking()
                        .CountAsync(d => d.KnowledgeBaseId == kb.Id, cancellationToken);
                    sb.AppendLine($"- Id={kb.Id} | Name={kb.Name} | Docs={docs}");
                }
            }

            sb.AppendLine();

            // Usuarios
            var users = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
            var members = await _db.TenantMembers.AsNoTracking()
                .CountAsync(m => m.TenantId == tenantId, cancellationToken);
            sb.AppendLine($"USUARIOS: Users={users}, TenantMembers={members}");

            sb.AppendLine();

            // Google
            var googleInMemory = string.Equals(
                _configuration["Google:UseInMemory"],
                "true",
                StringComparison.OrdinalIgnoreCase);
            var googleToken = !string.IsNullOrWhiteSpace(_configuration["Google:AccessToken"]);
            sb.AppendLine("GOOGLE WORKSPACE:");
            sb.AppendLine($"- UseInMemory={googleInMemory} (si true, correos/calendario son simulados en memoria)");
            sb.AppendLine($"- AccessToken configurado={(googleToken ? "sí" : "no")}");

            var oauth = await _db.OAuthConnections.AsNoTracking()
                .Where(o => o.TenantId == tenantId)
                .Select(o => o.Provider)
                .Distinct()
                .ToListAsync(cancellationToken);
            sb.AppendLine(oauth.Count == 0
                ? "- OAuth conexiones: (ninguna)"
                : $"- OAuth providers: {string.Join(", ", oauth)}");

            sb.AppendLine();
            sb.AppendLine("=== FIN ESTADO REAL ===");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo construir snapshot del sistema para Tenant {TenantId}", tenantId);
            return "=== ESTADO REAL NO DISPONIBLE (error al consultar DB) ===\n" + ex.Message;
        }
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var v = value.Trim();
        return v.Length <= max ? v : v[..max] + "…";
    }
}
