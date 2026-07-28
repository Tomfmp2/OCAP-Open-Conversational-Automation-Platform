using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Abstractions;

public record CreateAiProviderConfigurationDto(
    Guid TenantId,
    string ProviderName,
    string DisplayName,
    string ModelName,
    string ApiKey,
    string? BaseUrl = null,
    string? SettingsJson = null);

public record UpdateAiProviderConfigurationDto(
    string ModelName,
    string? ApiKey = null,
    string? BaseUrl = null,
    string? SettingsJson = null);

public record AiProviderConfigurationResponseDto(
    Guid Id,
    Guid TenantId,
    string ProviderName,
    string DisplayName,
    string ModelName,
    bool IsEnabled,
    string VaultSecretReference,
    string SettingsJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public interface IAiProviderConfigurationService
{
    Task<AiProviderConfigurationResponseDto> CreateConfigurationAsync(CreateAiProviderConfigurationDto dto, CancellationToken cancellationToken = default);
    Task<AiProviderConfigurationResponseDto?> GetConfigurationByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiProviderConfigurationResponseDto>> GetConfigurationsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<AiProviderConfigurationResponseDto?> UpdateConfigurationAsync(Guid tenantId, Guid id, UpdateAiProviderConfigurationDto dto, CancellationToken cancellationToken = default);
    Task<bool> SetStatusAsync(Guid tenantId, Guid id, bool enable, CancellationToken cancellationToken = default);
    Task<bool> DeleteConfigurationAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IAiProvider> GetRuntimeProviderForTenantAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default);
}
