using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Intelligence.Abstractions;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Api.Installation;

public interface IInstallationSetupService
{
    Task<InstallerStatusResponse> GetStatusAsync(CancellationToken cancellationToken);
    Task<InstallerSetupResponse> ApplyAsync(InstallerSetupRequest request, CancellationToken cancellationToken);
}

public sealed class InstallationSetupService : IInstallationSetupService
{
    private readonly InstallationArtifactStore _store;
    private readonly OCAPDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _configuration;
    private readonly IAiProviderConfigurationService _aiConfigs;
    private readonly ILogger<InstallationSetupService> _logger;

    public InstallationSetupService(
        InstallationArtifactStore store,
        OCAPDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        IAiProviderConfigurationService aiConfigs,
        ILogger<InstallationSetupService> logger)
    {
        _store = store;
        _db = db;
        _hasher = hasher;
        _configuration = configuration;
        _aiConfigs = aiConfigs;
        _logger = logger;
    }

    public async Task<InstallerStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        var completed = _store.IsCompleted(_configuration);
        var hasUsers = await _db.UserIdentities.IgnoreQueryFilters().AnyAsync(cancellationToken);
        var googleId = _configuration["Google:ClientId"];
        var aiKey = _configuration["AiProviders:OpenAI:ApiKey"]
                    ?? _configuration["AiProviders:Gemini:ApiKey"]
                    ?? _configuration["AiProviders:Claude:ApiKey"];

        return new InstallerStatusResponse
        {
            Completed = completed,
            Target = _configuration["Installation:Target"] ?? "Local",
            FrontendHostPort = _configuration.GetValue<int?>("Installation:FrontendHostPort"),
            ApiHostPort = _configuration.GetValue<int?>("Installation:ApiHostPort"),
            PublicApiUrl = _configuration["Installation:PublicApiUrl"],
            PublicPanelUrl = _configuration["Installation:PublicPanelUrl"],
            HasAdminUsers = hasUsers,
            GoogleConfigured = !string.IsNullOrWhiteSpace(googleId),
            AiConfigured = !string.IsNullOrWhiteSpace(aiKey) ||
                           !string.IsNullOrWhiteSpace(_configuration["AiProviders:Ollama:BaseUrl"]),
            ConfigPath = _store.ConfigDirectory
        };
    }

    public async Task<InstallerSetupResponse> ApplyAsync(InstallerSetupRequest request, CancellationToken cancellationToken)
    {
        var target = string.Equals(request.Target, "Web", StringComparison.OrdinalIgnoreCase) ? "Web" : "Local";
        var apiUrl = target == "Web"
            ? request.PublicApiUrl!.TrimEnd('/')
            : $"http://localhost:{request.ApiHostPort}";
        var panelUrl = target == "Web"
            ? request.PublicPanelUrl!.TrimEnd('/')
            : $"http://localhost:{request.FrontendHostPort}";
        var redirectUri = string.IsNullOrWhiteSpace(request.GoogleRedirectUri)
            ? $"{apiUrl}/api/integrations/Google/connect"
            : request.GoogleRedirectUri.Trim();

        var jwt = string.IsNullOrWhiteSpace(request.JwtSecretKey) || request.JwtSecretKey.Length < 32
            ? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            : request.JwtSecretKey;
        var vault = string.IsNullOrWhiteSpace(request.VaultMasterKey) || request.VaultMasterKey.Length < 32
            ? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            : request.VaultMasterKey;

        var envContent = InstallationArtifactStore.BuildEnvFile(request, target, apiUrl, panelUrl, redirectUri, jwt, vault);
        var document = InstallationArtifactStore.BuildInstallationDocument(request, target, apiUrl, panelUrl, redirectUri);
        await _store.WriteArtifactsAsync(request, envContent, document, cancellationToken);

        var adminCreated = await EnsureAdminAsync(request, cancellationToken);
        await EnsureAiProviderAsync(request, cancellationToken);

        var requiresRestart =
            request.FrontendHostPort != 3000 ||
            request.ApiHostPort != 5000 ||
            !string.Equals(request.PostgresPassword, "OcapSecurePass2026!", StringComparison.Ordinal) ||
            target == "Web";

        _logger.LogInformation("Instalación aplicada. AdminCreated={AdminCreated} RequiresRestart={RequiresRestart}",
            adminCreated, requiresRestart);

        var status = await GetStatusAsync(cancellationToken);
        status.Completed = true;
        status.Target = target;
        status.FrontendHostPort = request.FrontendHostPort;
        status.ApiHostPort = request.ApiHostPort;
        status.PublicApiUrl = apiUrl;
        status.PublicPanelUrl = panelUrl;

        return new InstallerSetupResponse
        {
            Success = true,
            RequiresRestart = requiresRestart,
            AdminCreated = adminCreated,
            Message = adminCreated
                ? "Instalación completada. Cuenta admin creada."
                : "Instalación completada. Ya existían usuarios; no se recreó el admin.",
            EnvFilePreview = envContent,
            RestartHint = requiresRestart
                ? "Copia config/generated.env a .env en la raíz del repo y ejecuta: docker compose up -d --force-recreate"
                : "Reinicio opcional. Los valores de Google/IA quedaron en config/installation.json.",
            Status = status
        };
    }

    private async Task<bool> EnsureAdminAsync(InstallerSetupRequest request, CancellationToken cancellationToken)
    {
        var hasUsers = await _db.UserIdentities.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (hasUsers)
            return false;

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var (hash, salt) = _hasher.HashPassword(request.AdminPassword);

        var tenant = new Tenant(tenantId, request.TenantName.Trim(), request.TenantSlug.Trim().ToLowerInvariant());
        var role = new Role(roleId, tenantId, "Admin", "Administrador del tenant", new[]
        {
            "Conversation.Read", "Conversation.Write", "Conversation.Delete",
            "Agent.Read", "Agent.Write", "Agent.Execute", "Tool.Execute",
            "Dashboard.Read", "Dashboard.Admin", "Deployment.Manage", "AI.Execute",
            "Settings.Manage", "OAuth.Manage", "Knowledge.Manage", "Workflow.Manage",
            "Security.Manage", "Channel.Manage"
        });
        var user = new UserIdentity(userId, tenantId, request.AdminEmail.Trim(), hash, salt, "Administrator");
        user.VerifyEmail();
        var userRole = new UserRole(Guid.NewGuid(), userId, roleId, tenantId);

        _db.Tenants.Add(tenant);
        _db.Roles.Add(role);
        _db.UserIdentities.Add(user);
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureAiProviderAsync(InstallerSetupRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().OrderBy(t => t.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
            return;

        var provider = InstallationArtifactStore.NormalizeAiProvider(request.AiProvider);
        var existing = await _aiConfigs.GetConfigurationsByTenantAsync(tenant.Id, cancellationToken);
        if (existing.Any(c => string.Equals(c.ProviderName, provider, StringComparison.OrdinalIgnoreCase)))
            return;

        if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(request.AiApiKey))
        {
            await _aiConfigs.CreateConfigurationAsync(
                new CreateAiProviderConfigurationDto(
                    tenant.Id,
                    provider,
                    $"{provider} (instalador)",
                    request.AiModelName,
                    request.AiApiKey ?? string.Empty,
                    request.AiBaseUrl),
                cancellationToken);
        }
    }
}
