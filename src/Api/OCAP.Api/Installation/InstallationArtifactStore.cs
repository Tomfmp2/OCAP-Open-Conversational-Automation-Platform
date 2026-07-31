using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OCAP.Api.Installation;

/// <summary>
/// Lee/escribe config/installation.json y config/generated.env (volumen montado).
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

    public async Task WriteArtifactsAsync(
        InstallerSetupRequest request,
        string envContent,
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
        _logger.LogInformation("Artefactos de instalación escritos en {Dir}", _configDirectory);
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

    public static JsonObject BuildInstallationDocument(InstallerSetupRequest request, string target, string apiUrl, string panelUrl, string redirectUri)
    {
        var provider = NormalizeAiProvider(request.AiProvider);
        var aiProviders = new JsonObject
        {
            [provider] = new JsonObject
            {
                ["ApiKey"] = request.AiApiKey ?? string.Empty,
                ["ModelName"] = request.AiModelName,
                ["BaseUrl"] = string.IsNullOrWhiteSpace(request.AiBaseUrl) ? null : request.AiBaseUrl
            }
        };

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
                ["AdminPassword"] = request.AdminPassword,
                ["TenantName"] = request.TenantName.Trim(),
                ["TenantSlug"] = request.TenantSlug.Trim().ToLowerInvariant()
            },
            ["Google"] = new JsonObject
            {
                ["ClientId"] = request.GoogleClientId ?? string.Empty,
                ["ClientSecret"] = request.GoogleClientSecret ?? string.Empty,
                ["RedirectUri"] = redirectUri,
                ["UseInMemory"] = false,
                ["AccessToken"] = string.Empty
            },
            ["AiProviders"] = aiProviders,
            ["Cors"] = new JsonObject
            {
                ["AllowedOrigins"] = new JsonArray(panelUrl)
            },
            ["Telegram"] = new JsonObject
            {
                ["BotToken"] = request.EnableTelegram ? (request.TelegramBotToken ?? string.Empty) : string.Empty
            }
        };

        return doc;
    }

    public static string BuildEnvFile(InstallerSetupRequest request, string target, string apiUrl, string panelUrl, string redirectUri, string jwt, string vault)
    {
        var provider = NormalizeAiProvider(request.AiProvider);
        var sb = new StringBuilder();
        sb.AppendLine("# Generated by OCAP Installer");
        sb.AppendLine($"# UTC: {DateTime.UtcNow:O}");
        sb.AppendLine();
        sb.AppendLine($"DEPLOYMENT_TARGET={target}");
        sb.AppendLine($"FRONTEND_HOST_PORT={request.FrontendHostPort}");
        sb.AppendLine($"API_HOST_PORT={request.ApiHostPort}");
        sb.AppendLine($"PUBLIC_API_URL={apiUrl}");
        sb.AppendLine($"PUBLIC_PANEL_URL={panelUrl}");
        sb.AppendLine();
        sb.AppendLine($"POSTGRES_HOST={request.PostgresHost}");
        sb.AppendLine($"POSTGRES_DB={request.PostgresDbName}");
        sb.AppendLine($"POSTGRES_USER={request.PostgresUsername}");
        sb.AppendLine($"POSTGRES_PASSWORD={request.PostgresPassword}");
        sb.AppendLine($"POSTGRES_HOST_PORT={request.PostgresPort}");
        sb.AppendLine();
        sb.AppendLine($"JWT_SECRET_KEY={jwt}");
        sb.AppendLine($"VAULT_MASTER_KEY={vault}");
        sb.AppendLine("EVENTBUS_PROVIDER=RabbitMQ");
        sb.AppendLine("RABBITMQ_USER=ocap");
        sb.AppendLine("RABBITMQ_PASSWORD=OcapRabbit2026!");
        sb.AppendLine("STORAGE_PROVIDER=Local");
        sb.AppendLine("ASPNETCORE_ENVIRONMENT=Production");
        sb.AppendLine();
        sb.AppendLine($"BOOTSTRAP_ADMIN_EMAIL={request.AdminEmail.Trim()}");
        sb.AppendLine($"BOOTSTRAP_ADMIN_PASSWORD={request.AdminPassword}");
        sb.AppendLine($"BOOTSTRAP_TENANT_NAME={request.TenantName.Trim()}");
        sb.AppendLine($"BOOTSTRAP_TENANT_SLUG={request.TenantSlug.Trim().ToLowerInvariant()}");
        sb.AppendLine();
        sb.AppendLine($"Google__ClientId={request.GoogleClientId}");
        sb.AppendLine($"Google__ClientSecret={request.GoogleClientSecret}");
        sb.AppendLine($"Google__RedirectUri={redirectUri}");
        sb.AppendLine("Google__UseInMemory=false");
        sb.AppendLine();
        sb.AppendLine($"AiProviders__{provider}__ApiKey={request.AiApiKey}");
        sb.AppendLine($"AiProviders__{provider}__ModelName={request.AiModelName}");
        if (!string.IsNullOrWhiteSpace(request.AiBaseUrl))
            sb.AppendLine($"AiProviders__{provider}__BaseUrl={request.AiBaseUrl}");
        sb.AppendLine();
        sb.AppendLine($"Cors__AllowedOrigins__0={panelUrl}");
        sb.AppendLine("NEXT_PUBLIC_API_URL=");
        sb.AppendLine("API_INTERNAL_URL=http://ocap-api:5000");
        sb.AppendLine();
        sb.AppendLine($"EVOLUTION_API_KEY={request.EvolutionApiKey ?? "EvolutionSecretApiKey"}");
        sb.AppendLine($"EVOLUTION_API_URL={request.EvolutionApiUrl ?? "http://localhost:8088"}");
        if (request.EnableTelegram && !string.IsNullOrWhiteSpace(request.TelegramBotToken))
            sb.AppendLine($"Telegram__BotToken={request.TelegramBotToken}");
        return sb.ToString();
    }

    public static string NormalizeAiProvider(string? provider) =>
        (provider ?? "OpenAI").Trim() switch
        {
            "Gemini" => "Gemini",
            "Claude" => "Claude",
            "Ollama" => "Ollama",
            _ => "OpenAI"
        };
}
