using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OCAP.Api.Installation;

/// <summary>
/// Lee/escribe config/installation.json y fusiona claves en .env / generated.env.
/// </summary>
public sealed class InstallationArtifactStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    private readonly string _configDirectory;
    private readonly ILogger<InstallationArtifactStore> _logger;

    public InstallationArtifactStore(IConfiguration configuration, ILogger<InstallationArtifactStore> logger)
    {
        _logger = logger;
        var configured = configuration["Installation:ConfigPath"];
        _configDirectory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "config")
            : configured;
    }

    public string ConfigDirectory => _configDirectory;
    public string InstallationJsonPath => Path.Combine(_configDirectory, "installation.json");
    public string GeneratedEnvPath => Path.Combine(_configDirectory, "generated.env");
    public string ProjectDotEnvPath
    {
        get
        {
            // Prefer .env en la raíz del repo (junto a .env.example / docker-compose).
            var dir = new DirectoryInfo(_configDirectory);
            for (var i = 0; i < 6 && dir is not null; i++)
            {
                var candidate = Path.Combine(dir.FullName, ".env");
                var example = Path.Combine(dir.FullName, ".env.example");
                if (File.Exists(candidate) || File.Exists(example) || File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
                    return candidate;
                dir = dir.Parent;
            }

            return Path.GetFullPath(Path.Combine(_configDirectory, "..", ".env"));
        }
    }

    public bool IsCompleted(IConfiguration configuration)
    {
        if (configuration.GetValue("Installation:Completed", false))
            return true;

        if (!File.Exists(InstallationJsonPath))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(InstallationJsonPath));
            if (doc.RootElement.TryGetProperty("Installation", out var section) &&
                section.TryGetProperty("Completed", out var completed))
            {
                return completed.ValueKind == JsonValueKind.True ||
                       (completed.ValueKind == JsonValueKind.String &&
                        bool.TryParse(completed.GetString(), out var b) && b);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer {Path}", InstallationJsonPath);
        }

        return false;
    }

    public async Task<string?> ReadExistingDotEnvAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(ProjectDotEnvPath))
            return await File.ReadAllTextAsync(ProjectDotEnvPath, cancellationToken);
        if (File.Exists(GeneratedEnvPath))
            return await File.ReadAllTextAsync(GeneratedEnvPath, cancellationToken);
        return null;
    }

    public static IReadOnlyDictionary<string, string> ParseEnvFile(string? content)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(content))
            return map;

        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;
            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = trimmed[..eq].Trim();
            var value = trimmed[(eq + 1)..];
            map[key] = value;
        }

        return map;
    }

    /// <summary>
    /// Fusiona claves nuevas sobre el .env existente: preserva claves no tocadas (p. ej. UseInMemory local).
    /// </summary>
    public static (string Content, IReadOnlyList<string> UpdatedKeys) MergeEnvContent(
        string? existingContent,
        IReadOnlyDictionary<string, string> incoming)
    {
        var existing = new Dictionary<string, string>(ParseEnvFile(existingContent), StringComparer.OrdinalIgnoreCase);
        var updated = new List<string>();
        foreach (var (key, value) in incoming)
        {
            if (!existing.TryGetValue(key, out var prev) || !string.Equals(prev, value, StringComparison.Ordinal))
                updated.Add(key);
            existing[key] = value;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# OCAP environment (merged by installer)");
        sb.AppendLine($"# UTC: {DateTime.UtcNow:O}");
        sb.AppendLine();
        foreach (var (key, value) in existing.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"{key}={value}");

        return (sb.ToString(), updated);
    }

    public async Task<(bool DotEnvWritten, string DotEnvPath, IReadOnlyList<string> UpdatedKeys)> WriteArtifactsAsync(
        string envContent,
        IReadOnlyList<string> updatedKeys,
        JsonObject installationDocument,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_configDirectory);
        await File.WriteAllTextAsync(GeneratedEnvPath, envContent, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(
            InstallationJsonPath,
            installationDocument.ToJsonString(JsonOptions),
            Encoding.UTF8,
            cancellationToken);

        var dotEnvWritten = false;
        var dotEnvPath = ProjectDotEnvPath;
        try
        {
            var projectRoot = Path.GetDirectoryName(dotEnvPath);
            if (!string.IsNullOrWhiteSpace(projectRoot))
                Directory.CreateDirectory(projectRoot);
            await File.WriteAllTextAsync(dotEnvPath, envContent, Encoding.UTF8, cancellationToken);
            dotEnvWritten = true;
            _logger.LogInformation("Archivo .env del proyecto fusionado en {Path} ({Count} claves)",
                dotEnvPath, updatedKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo escribir .env en {Path}. Queda disponible en {Generated}",
                dotEnvPath, GeneratedEnvPath);
        }

        _logger.LogInformation(
            "Artefactos de instalación escritos en {Dir} (installation.json, generated.env)",
            _configDirectory);
        return (dotEnvWritten, dotEnvPath, updatedKeys);
    }

    public async Task MergeGoogleAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_configDirectory);
        JsonObject root;
        if (File.Exists(InstallationJsonPath))
        {
            var existing = await File.ReadAllTextAsync(InstallationJsonPath, cancellationToken);
            root = JsonNode.Parse(existing) as JsonObject ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var google = root["Google"] as JsonObject ?? new JsonObject();
        google["AccessToken"] = accessToken;
        google["UseInMemory"] = false;
        root["Google"] = google;

        await File.WriteAllTextAsync(
            InstallationJsonPath,
            root.ToJsonString(JsonOptions),
            Encoding.UTF8,
            cancellationToken);
    }

    public static string NormalizeTarget(string? target)
    {
        if (string.Equals(target, "Web", StringComparison.OrdinalIgnoreCase))
            return "Web";
        if (string.Equals(target, "Local", StringComparison.OrdinalIgnoreCase))
            return "Local";
        return "Dev";
    }

    public static JsonObject BuildInstallationDocument(
        InstallerSetupRequest request,
        string target,
        string apiUrl,
        string panelUrl,
        string redirectUri)
    {
        var provider = NormalizeAiProvider(request.AiProvider);
        var googleInMemory = !request.EnableGoogleWorkspace;
        // Nunca escribir ApiKey aquí: este JSON se carga en IConfiguration y pisaría el .env
        // (antes se guardaba "***" y Gemini rechazaba la key como inválida).
        var providerConfig = new JsonObject
        {
            ["ApiKeyConfigured"] = !string.IsNullOrWhiteSpace(request.AiApiKey),
            ["ModelName"] = request.AiModelName,
        };
        if (!string.IsNullOrWhiteSpace(request.AiBaseUrl))
            providerConfig["BaseUrl"] = request.AiBaseUrl;

        var doc = new JsonObject
        {
            ["Installation"] = new JsonObject
            {
                ["Completed"] = true,
                ["Target"] = target,
                ["FrontendHostPort"] = request.FrontendHostPort,
                ["ApiHostPort"] = request.ApiHostPort,
                ["PublicApiUrl"] = apiUrl,
                ["PublicPanelUrl"] = panelUrl,
                ["CompletedAtUtc"] = DateTime.UtcNow.ToString("O")
            },
            ["Bootstrap"] = new JsonObject
            {
                ["Enabled"] = true,
                ["AdminEmail"] = request.AdminEmail.Trim(),
                ["TenantName"] = request.TenantName.Trim(),
                ["TenantSlug"] = request.TenantSlug.Trim().ToLowerInvariant()
            },
            ["Google"] = new JsonObject
            {
                ["ClientId"] = request.EnableGoogleWorkspace ? (request.GoogleClientId ?? string.Empty) : string.Empty,
                ["RedirectUri"] = redirectUri,
                ["UseInMemory"] = googleInMemory,
                ["Configured"] = request.EnableGoogleWorkspace
            },
            ["AiProviders"] = new JsonObject
            {
                ["PreferredProvider"] = provider,
                [provider] = providerConfig
            },
            ["Cors"] = new JsonObject
            {
                ["AllowedOrigins"] = new JsonArray(panelUrl)
            },
            ["Telegram"] = new JsonObject
            {
                ["BotTokenConfigured"] = request.EnableTelegram && !string.IsNullOrWhiteSpace(request.TelegramBotToken)
            },
            ["WhatsApp"] = new JsonObject
            {
                ["Enabled"] = request.EnableWhatsApp,
                ["EvolutionApiUrl"] = request.EnableWhatsApp ? (request.EvolutionApiUrl ?? string.Empty) : string.Empty
            }
        };

        if (target == "Dev")
        {
            doc["UseInMemory"] = true;
        }

        return doc;
    }

    public static Dictionary<string, string> BuildEnvUpdates(
        InstallerSetupRequest request,
        string target,
        string apiUrl,
        string panelUrl,
        string redirectUri,
        string jwt,
        string vault)
    {
        var provider = NormalizeAiProvider(request.AiProvider);
        var googleInMemory = !request.EnableGoogleWorkspace;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEPLOYMENT_TARGET"] = target,
            ["FRONTEND_HOST_PORT"] = request.FrontendHostPort.ToString(),
            ["API_HOST_PORT"] = request.ApiHostPort.ToString(),
            ["PUBLIC_API_URL"] = apiUrl,
            ["PUBLIC_PANEL_URL"] = panelUrl,
            ["JWT_SECRET_KEY"] = jwt,
            ["VAULT_MASTER_KEY"] = vault,
            ["BOOTSTRAP_ADMIN_EMAIL"] = request.AdminEmail.Trim(),
            ["BOOTSTRAP_ADMIN_PASSWORD"] = request.AdminPassword,
            ["BOOTSTRAP_TENANT_NAME"] = request.TenantName.Trim(),
            ["BOOTSTRAP_TENANT_SLUG"] = request.TenantSlug.Trim().ToLowerInvariant(),
            ["Google__RedirectUri"] = redirectUri,
            ["Google__UseInMemory"] = googleInMemory ? "true" : "false",
            ["AiProviders__PreferredProvider"] = provider,
            [$"AiProviders__{provider}__ModelName"] = request.AiModelName,
            ["Cors__AllowedOrigins__0"] = panelUrl
        };

        if (target == "Dev")
        {
            map["ASPNETCORE_ENVIRONMENT"] = "Development";
            map["UseInMemory"] = "true";
            map["EVENTBUS_PROVIDER"] = "InMemory";
            map["NEXT_PUBLIC_API_URL"] = apiUrl;
        }
        else if (target == "Local")
        {
            map["ASPNETCORE_ENVIRONMENT"] = "Production";
            map["UseInMemory"] = "false";
            map["EVENTBUS_PROVIDER"] = "RabbitMQ";
            map["RABBITMQ_USER"] = "ocap";
            map["RABBITMQ_PASSWORD"] = "OcapRabbit2026!";
            map["STORAGE_PROVIDER"] = "Local";
            map["POSTGRES_HOST"] = request.PostgresHost;
            map["POSTGRES_DB"] = request.PostgresDbName;
            map["POSTGRES_USER"] = request.PostgresUsername;
            map["POSTGRES_PASSWORD"] = request.PostgresPassword;
            map["POSTGRES_HOST_PORT"] = request.PostgresPort.ToString();
            map["API_INTERNAL_URL"] = "http://ocap-api:5000";
            map["NEXT_PUBLIC_API_URL"] = string.Empty;
        }
        else
        {
            map["ASPNETCORE_ENVIRONMENT"] = "Production";
            map["UseInMemory"] = "false";
            map["EVENTBUS_PROVIDER"] = "RabbitMQ";
            map["STORAGE_PROVIDER"] = "Local";
            map["POSTGRES_HOST"] = request.PostgresHost;
            map["POSTGRES_DB"] = request.PostgresDbName;
            map["POSTGRES_USER"] = request.PostgresUsername;
            map["POSTGRES_PASSWORD"] = request.PostgresPassword;
            map["POSTGRES_HOST_PORT"] = request.PostgresPort.ToString();
            map["API_INTERNAL_URL"] = "http://ocap-api:5000";
            map["NEXT_PUBLIC_API_URL"] = string.Empty;
        }

        if (request.EnableGoogleWorkspace)
        {
            map["Google__ClientId"] = request.GoogleClientId ?? string.Empty;
            map["Google__ClientSecret"] = request.GoogleClientSecret ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(request.AiApiKey))
            map[$"AiProviders__{provider}__ApiKey"] = request.AiApiKey!;

        if (!string.IsNullOrWhiteSpace(request.AiBaseUrl))
            map[$"AiProviders__{provider}__BaseUrl"] = request.AiBaseUrl!;

        if (request.EnableWhatsApp)
        {
            map["EVOLUTION_API_KEY"] = request.EvolutionApiKey ?? string.Empty;
            map["EVOLUTION_API_URL"] = request.EvolutionApiUrl ?? "http://localhost:8088";
        }

        if (request.EnableTelegram && !string.IsNullOrWhiteSpace(request.TelegramBotToken))
            map["Telegram__BotToken"] = request.TelegramBotToken!;

        return map;
    }

    public static string NormalizeAiProvider(string? provider) =>
        (provider ?? "Gemini").Trim() switch
        {
            "Gemini" => "Gemini",
            "Claude" => "Claude",
            "Ollama" => "Ollama",
            "OpenAI" => "OpenAI",
            _ => "Gemini"
        };
}
