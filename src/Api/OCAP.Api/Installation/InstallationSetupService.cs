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
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

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
            Target = _configuration["Installation:Target"]
                     ?? (_configuration.GetValue("UseInMemory", false) ? "Dev" : "Local"),
            FrontendHostPort = _configuration.GetValue<int?>("Installation:FrontendHostPort"),
            ApiHostPort = _configuration.GetValue<int?>("Installation:ApiHostPort")
                          ?? (_configuration.GetValue("UseInMemory", false) ? 5229 : 5000),
            PublicApiUrl = _configuration["Installation:PublicApiUrl"],
            PublicPanelUrl = _configuration["Installation:PublicPanelUrl"],
            HasAdminUsers = hasUsers,
            GoogleConfigured = !string.IsNullOrWhiteSpace(googleId),
            AiConfigured = !string.IsNullOrWhiteSpace(aiKey) ||
                           !string.IsNullOrWhiteSpace(_configuration["AiProviders:Ollama:BaseUrl"]),
            ConfigPath = _store.ConfigDirectory,
            AllowsAnonymousSetup = !completed
        };
    }

    public async Task<InstallerSetupResponse> ApplyAsync(InstallerSetupRequest request, CancellationToken cancellationToken)
    {
        var target = InstallationArtifactStore.NormalizeTarget(request.Target);

        if (target == "Dev")
        {
            request.FrontendHostPort = 3000;
            request.ApiHostPort = 5229;
        }
        else if (target == "Local")
        {
            request.FrontendHostPort = 3000;
            request.ApiHostPort = 5000;
            request.PostgresHost = string.IsNullOrWhiteSpace(request.PostgresHost) ? "localhost" : request.PostgresHost;
            request.PostgresPort = request.PostgresPort <= 0 ? 5433 : request.PostgresPort;
            if (string.IsNullOrWhiteSpace(request.PostgresDbName))
                request.PostgresDbName = "ocap_db";
            if (string.IsNullOrWhiteSpace(request.PostgresUsername))
                request.PostgresUsername = "ocap_user";
            request.PostgresPassword = "OcapSecurePass2026!";
        }

        var apiUrl = target switch
        {
            "Web" => request.PublicApiUrl!.TrimEnd('/'),
            "Local" => "http://localhost:5000",
            _ => "http://localhost:5229"
        };
        var panelUrl = target switch
        {
            "Web" => request.PublicPanelUrl!.TrimEnd('/'),
            _ => "http://localhost:3000"
        };
        var redirectUri = string.IsNullOrWhiteSpace(request.GoogleRedirectUri)
            ? $"{apiUrl}/api/integrations/Google/connect"
            : request.GoogleRedirectUri.Trim();

        var existingEnv = await _store.ReadExistingDotEnvAsync(cancellationToken);
        var existingMap = InstallationArtifactStore.ParseEnvFile(existingEnv);

        var jwt = ResolveSecret(
            request.JwtSecretKey,
            existingMap,
            "JWT_SECRET_KEY",
            () => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        var vault = ResolveSecret(
            request.VaultMasterKey,
            existingMap,
            "VAULT_MASTER_KEY",
            () => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

        var updates = InstallationArtifactStore.BuildEnvUpdates(
            request, target, apiUrl, panelUrl, redirectUri, jwt, vault);
        var (envContent, updatedKeys) = InstallationArtifactStore.MergeEnvContent(existingEnv, updates);
        var document = InstallationArtifactStore.BuildInstallationDocument(
            request, target, apiUrl, panelUrl, redirectUri);
        var (dotEnvWritten, dotEnvPath, _) =
            await _store.WriteArtifactsAsync(envContent, updatedKeys, document, cancellationToken);

        var (adminCreated, adminUpdated) = await EnsureAdminAsync(request, cancellationToken);
        await EnsureAiProviderAsync(request, cancellationToken);

        var requiresRestart = target != "Dev";

        _logger.LogInformation(
            "Instalación aplicada. Target={Target} AdminCreated={AdminCreated} AdminUpdated={AdminUpdated} Keys={KeyCount} DotEnvWritten={DotEnvWritten}",
            target, adminCreated, adminUpdated, updatedKeys.Count, dotEnvWritten);

        var status = await GetStatusAsync(cancellationToken);
        status.Completed = true;
        status.Target = target;
        status.FrontendHostPort = request.FrontendHostPort;
        status.ApiHostPort = request.ApiHostPort;
        status.PublicApiUrl = apiUrl;
        status.PublicPanelUrl = panelUrl;
        status.AllowsAnonymousSetup = false;

        var message = adminCreated
            ? $"Configuración guardada. Admin creado: {request.AdminEmail.Trim()}."
            : adminUpdated
                ? $"Configuración guardada. Admin actualizado: {request.AdminEmail.Trim()} (usa esa contraseña en /login)."
                : "Configuración de producto guardada.";

        if (dotEnvWritten)
            message += " Se fusionó .env (claves previas no tocadas se conservan).";

        message += target switch
        {
            "Dev" => " Panel en http://localhost:3000 — API en http://localhost:5229 (reinicia ocap-dev para cargar .env).",
            "Local" => " Panel en http://localhost:3000 — API en http://localhost:5000.",
            _ => $" Panel: {panelUrl} — API: {apiUrl}."
        };

        return new InstallerSetupResponse
        {
            Success = true,
            RequiresRestart = requiresRestart,
            AdminCreated = adminCreated,
            AdminUpdated = adminUpdated,
            DotEnvWritten = dotEnvWritten,
            Message = message,
            EnvKeysUpdated = updatedKeys,
            EnvFilePath = _store.GeneratedEnvPath,
            DotEnvPath = dotEnvPath,
            RestartHint = target switch
            {
                "Dev" => "Reinicia la API (scripts/ocap-dev.ps1) para aplicar variables nuevas del .env.",
                "Local" => "Stack Compose: ./scripts/ocap-up.sh. Reset total: docker compose down -v && ./scripts/ocap-up.sh",
                _ => "Si nginx/CORS no reflejan las URLs nuevas, ejecuta ./scripts/ocap-up.sh en el servidor."
            },
            Status = status
        };
    }

    private static string ResolveSecret(
        string? requestValue,
        IReadOnlyDictionary<string, string> existing,
        string envKey,
        Func<string> generate)
    {
        if (!string.IsNullOrWhiteSpace(requestValue) && requestValue.Length >= 32)
            return requestValue;
        if (existing.TryGetValue(envKey, out var existingValue) &&
            !string.IsNullOrWhiteSpace(existingValue) &&
            existingValue.Length >= 32)
            return existingValue;
        return generate();
    }

    private async Task<(bool Created, bool Updated)> EnsureAdminAsync(
        InstallerSetupRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.AdminEmail.Trim().ToLowerInvariant();
        var (hash, salt) = _hasher.HashPassword(request.AdminPassword);

        var existingUser = await _db.UserIdentities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser is null)
        {
            existingUser = await _db.UserIdentities.IgnoreQueryFilters()
                .OrderBy(u => u.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (existingUser is not null)
        {
            if (!string.Equals(existingUser.Email, email, StringComparison.OrdinalIgnoreCase))
                existingUser.ChangeEmail(email);
            existingUser.UpdatePassword(hash, salt);
            existingUser.Unlock();
            existingUser.Activate();
            existingUser.VerifyEmail();
            await _db.SaveChangesAsync(cancellationToken);
            return (false, true);
        }

        var tenantId = Guid.TryParse(_configuration["Bootstrap:TenantId"], out var configuredTenantId)
                       && configuredTenantId != Guid.Empty
            ? configuredTenantId
            : DefaultTenantId;

        var existingTenant = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (existingTenant is null)
        {
            var slug = request.TenantSlug.Trim().ToLowerInvariant();
            existingTenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
        }

        if (existingTenant is null)
        {
            existingTenant = new Tenant(tenantId, request.TenantName.Trim(), request.TenantSlug.Trim().ToLowerInvariant());
            _db.Tenants.Add(existingTenant);
        }

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role(roleId, existingTenant.Id, "Admin", "Administrador del tenant", new[]
        {
            "Conversation.Read", "Conversation.Write", "Conversation.Delete",
            "Agent.Read", "Agent.Write", "Agent.Execute", "Tool.Execute",
            "Dashboard.Read", "Dashboard.Admin", "Deployment.Manage", "AI.Execute",
            "Settings.Manage", "OAuth.Manage", "Knowledge.Manage", "Workflow.Manage",
            "Security.Manage", "Channel.Manage"
        });
        var user = new UserIdentity(userId, existingTenant.Id, email, hash, salt, "Administrator");
        user.VerifyEmail();
        var userRole = new UserRole(Guid.NewGuid(), userId, roleId, existingTenant.Id);

        _db.Roles.Add(role);
        _db.UserIdentities.Add(user);
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, false);
    }

    private async Task EnsureAiProviderAsync(InstallerSetupRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().OrderBy(t => t.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
            return;

        var provider = InstallationArtifactStore.NormalizeAiProvider(request.AiProvider);
        var existing = await _db.AiProviderConfigurations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenant.Id && c.ProviderName.ToLower() == provider.ToLower(),
                cancellationToken);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.AiApiKey) ||
                string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
            {
                await _aiConfigs.UpdateConfigurationAsync(
                    tenant.Id,
                    existing.Id,
                    new UpdateAiProviderConfigurationDto(
                        request.AiModelName,
                        request.AiApiKey,
                        request.AiBaseUrl),
                    cancellationToken);
            }

            return;
        }

        if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(request.AiApiKey))
        {
            try
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
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex,
                    "Proveedor IA {Provider} ya existía para tenant {TenantId}; se omite recreación.",
                    provider, tenant.Id);
            }
        }
    }
}
