using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;
using OCAP.Security.Abstractions;

namespace OCAP.Infrastructure.Services;

// Servicio de infraestructura encargado de la gestión persistente y resolución en tiempo de ejecución de configuraciones de proveedores de IA aislados por Tenant.
public class AiProviderConfigurationService : IAiProviderConfigurationService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ICredentialVault _credentialVault;
    private readonly IAiProviderRegistry _providerRegistry;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProviderConfigurationService> _logger;

    public AiProviderConfigurationService(
        OCAPDbContext dbContext,
        ICredentialVault credentialVault,
        IAiProviderRegistry providerRegistry,
        IConfiguration configuration,
        ILogger<AiProviderConfigurationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _credentialVault = credentialVault ?? throw new ArgumentNullException(nameof(credentialVault));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AiProviderConfigurationResponseDto> CreateConfigurationAsync(CreateAiProviderConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.TenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(dto.TenantId));
        if (string.IsNullOrWhiteSpace(dto.ProviderName)) throw new ArgumentException("ProviderName is required.", nameof(dto.ProviderName));

        var normalizedProvider = dto.ProviderName.Trim();

        // 1. Guardar la API Key de forma cifrada en el Credential Vault
        var vaultRef = await _credentialVault.StoreSecretAsync(
            dto.TenantId,
            $"{normalizedProvider}_ApiKey_{Guid.NewGuid():N}",
            dto.ApiKey ?? string.Empty,
            cancellationToken);

        // 2. Armar SettingsJson si se proporcionó BaseUrl
        var settingsDict = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(dto.BaseUrl))
        {
            settingsDict["BaseUrl"] = dto.BaseUrl.Trim();
        }

        var finalSettingsJson = !string.IsNullOrWhiteSpace(dto.SettingsJson)
            ? dto.SettingsJson
            : JsonSerializer.Serialize(settingsDict);

        // 3. Crear entidad de dominio AiProviderConfiguration
        var entity = new AiProviderConfiguration(
            dto.TenantId,
            normalizedProvider,
            dto.DisplayName,
            dto.ModelName,
            vaultRef,
            finalSettingsJson);

        await _dbContext.AiProviderConfigurations.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Creada nueva configuración de proveedor de IA {ProviderName} para Tenant {TenantId}.", normalizedProvider, dto.TenantId);

        return MapToDto(entity);
    }

    public async Task<AiProviderConfigurationResponseDto?> GetConfigurationByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AiProviderConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<IReadOnlyList<AiProviderConfigurationResponseDto>> GetConfigurationsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: el caller ya pasa tenantId explícito (p. ej. instalador anónimo).
        var list = await _dbContext.AiProviderConfigurations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<AiProviderConfigurationResponseDto?> UpdateConfigurationAsync(Guid tenantId, Guid id, UpdateAiProviderConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AiProviderConfigurations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

        if (entity == null) return null;

        var vaultRef = entity.VaultSecretReference;

        // Si se proporcionó una nueva API Key, guardarla cifrada
        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            vaultRef = await _credentialVault.StoreSecretAsync(
                tenantId,
                $"{entity.ProviderName}_ApiKey_{Guid.NewGuid():N}",
                dto.ApiKey.Trim(),
                cancellationToken);
        }

        var settingsJson = dto.SettingsJson;
        if (settingsJson == null && !string.IsNullOrWhiteSpace(dto.BaseUrl))
        {
            var dict = new Dictionary<string, string> { ["BaseUrl"] = dto.BaseUrl.Trim() };
            settingsJson = JsonSerializer.Serialize(dict);
        }

        entity.UpdateConfiguration(dto.ModelName, vaultRef, settingsJson);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Actualizada configuración de proveedor de IA {Id} para Tenant {TenantId}.", id, tenantId);

        return MapToDto(entity);
    }

    public async Task<bool> SetStatusAsync(Guid tenantId, Guid id, bool enable, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AiProviderConfigurations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

        if (entity == null) return false;

        if (enable) entity.Enable();
        else entity.Disable();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteConfigurationAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AiProviderConfigurations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

        if (entity == null) return false;

        if (!string.IsNullOrWhiteSpace(entity.VaultSecretReference))
        {
            await _credentialVault.DeleteSecretAsync(tenantId, entity.VaultSecretReference, cancellationToken);
        }

        _dbContext.AiProviderConfigurations.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Eliminada configuración de proveedor de IA {Id} para Tenant {TenantId}.", id, tenantId);
        return true;
    }

    public async Task<IAiProvider?> GetRuntimeProviderForTenantAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        // 1. Buscar configuraciones habilitadas para el tenant
        var query = _dbContext.AiProviderConfigurations
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsEnabled);

        AiProviderConfiguration? config = null;

        if (!string.IsNullOrWhiteSpace(preferredProvider))
        {
            config = await query.FirstOrDefaultAsync(c => c.ProviderName.ToLower() == preferredProvider.Trim().ToLower(), cancellationToken);
        }

        config ??= await query.FirstOrDefaultAsync(cancellationToken);

        if (config == null)
        {
            _logger.LogWarning(
                "Sin configuración de IA en DB para Tenant {TenantId}. Se usará el proveedor estático (.env/appsettings).",
                tenantId);
            return null;
        }

        // 2. Recuperar la API key cifrada desde Credential Vault
        var apiKey = string.Empty;
        if (!string.IsNullOrWhiteSpace(config.VaultSecretReference))
        {
            apiKey = await _credentialVault.RetrieveSecretAsync(tenantId, config.VaultSecretReference, cancellationToken) ?? string.Empty;
        }

        // En Dev / UseInMemory: si hay key en .env, tiene prioridad sobre un vault viejo/incorrecto
        // (el instalador o un "Registrar" con key de prueba suele dejar el vault inválido).
        var envKey = ResolveEnvApiKey(config.ProviderName);
        var useInMemory = _configuration.GetValue("UseInMemory", false);
        var isDev = string.Equals(
            _configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(envKey) && (useInMemory || isDev))
        {
            if (!string.Equals(apiKey.Trim(), envKey.Trim(), StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Dev: usando AiProviders__{Provider}__ApiKey del .env en lugar del vault (tenant {TenantId}).",
                    config.ProviderName,
                    tenantId);
            }
            apiKey = envKey.Trim();
        }

        // Si el vault está vacío y no hay .env, no forzar un proveedor roto: caer al estático del DI.
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "Configuración IA {Provider} sin API key (vault/.env) para Tenant {TenantId}. Fallback a estático.",
                config.ProviderName,
                tenantId);
            return null;
        }

        // Parsear BaseUrl si existe en SettingsJson
        string? baseUrl = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(config.SettingsJson))
            {
                using var doc = JsonDocument.Parse(config.SettingsJson);
                if (doc.RootElement.TryGetProperty("BaseUrl", out var bUrl))
                {
                    baseUrl = bUrl.GetString();
                }
            }
        }
        catch { /* ignore */ }

        // Preferir modelo del entorno si la config guardada es un modelo obsoleto.
        var modelName = config.ModelName;
        var envModel = _configuration[$"AiProviders:{config.ProviderName}:ModelName"];
        if (IsObsoleteGeminiModel(modelName))
        {
            modelName = !string.IsNullOrWhiteSpace(envModel) ? envModel.Trim() : "gemini-3.5-flash";
            _logger.LogWarning(
                "Modelo Gemini obsoleto '{Old}' en config {Id}; usando {New} en runtime.",
                config.ModelName, config.Id, modelName);

            // Persistimos la corrección para que la UI deje de mostrar 1.5-flash.
            try
            {
                var tracked = await _dbContext.AiProviderConfigurations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == config.Id, cancellationToken);
                if (tracked != null)
                {
                    tracked.UpdateConfiguration(modelName, tracked.VaultSecretReference, tracked.SettingsJson);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "No se pudo persistir migración de modelo Gemini.");
            }
        }

        return _providerRegistry.CreateDynamicProvider(config.ProviderName, modelName, apiKey, baseUrl);
    }

    private string? ResolveEnvApiKey(string providerName)
    {
        var name = providerName.Trim();
        return _configuration[$"AiProviders:{name}:ApiKey"]
               ?? _configuration[$"AiProviders__{name}__ApiKey"];
    }

    private static bool IsObsoleteGeminiModel(string? model) =>
        string.IsNullOrWhiteSpace(model) ||
        model.Contains("1.5", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(model, "gemini-2.0-flash", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(model, "gemini-2.0-flash-001", StringComparison.OrdinalIgnoreCase);

    private static AiProviderConfigurationResponseDto MapToDto(AiProviderConfiguration config)
    {
        return new AiProviderConfigurationResponseDto(
            config.Id,
            config.TenantId,
            config.ProviderName,
            config.DisplayName,
            config.ModelName,
            config.IsEnabled,
            config.VaultSecretReference,
            config.SettingsJson,
            config.CreatedAtUtc,
            config.UpdatedAtUtc);
    }
}
