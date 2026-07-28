namespace OCAP.Intelligence.Domain;

public class AiProviderConfiguration
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public string VaultSecretReference { get; private set; } = string.Empty;
    public string SettingsJson { get; private set; } = "{}";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private AiProviderConfiguration() { }

    public AiProviderConfiguration(
        Guid tenantId,
        string providerName,
        string displayName,
        string modelName,
        string vaultSecretReference,
        string? settingsJson = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId != Guid.Empty ? tenantId : throw new ArgumentException("TenantId is required.", nameof(tenantId));
        ProviderName = !string.IsNullOrWhiteSpace(providerName) ? providerName.Trim() : throw new ArgumentException("ProviderName is required.", nameof(providerName));
        DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : providerName;
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "default";
        VaultSecretReference = vaultSecretReference ?? string.Empty;
        SettingsJson = settingsJson ?? "{}";
        IsEnabled = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateConfiguration(string modelName, string vaultSecretReference, string? settingsJson = null)
    {
        if (!string.IsNullOrWhiteSpace(modelName)) ModelName = modelName.Trim();
        if (!string.IsNullOrWhiteSpace(vaultSecretReference)) VaultSecretReference = vaultSecretReference;
        if (settingsJson != null) SettingsJson = settingsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
