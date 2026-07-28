namespace OCAP.Security.Domain.Entities;

// Entidad de dominio multi-tenant para la configuración y gestión de conexiones de canales externos.
public class ChannelConnection
{
    // Identificador único de la conexión del canal.
    public Guid Id { get; private set; }

    // Identificador del Tenant al que pertenece la conexión.
    public Guid TenantId { get; private set; }

    // Proveedor del canal (ej. "Telegram", "WhatsApp", "GoogleWorkspace", "WebChat", "Slack", "MicrosoftTeams").
    public string Provider { get; private set; } = string.Empty;

    // Nombre descriptivo asignado a la conexión (ej. "WhatsApp Soporte Principal").
    public string DisplayName { get; private set; } = string.Empty;

    // Estado de activación de la conexión.
    public bool Enabled { get; private set; }

    // Referencia segura/cifrada al vault de credenciales (NUNCA credenciales en texto plano).
    public string CredentialsReference { get; private set; } = string.Empty;

    // Metadatos de configuración no sensibles (ej. Webhook URL, bot username, opciones del proveedor).
    public Dictionary<string, string> ConfigurationMetadata { get; private set; } = new();

    // Fecha UTC de creación del registro.
    public DateTime CreatedAtUtc { get; private set; }

    // Fecha UTC de última actualización.
    public DateTime? UpdatedAtUtc { get; private set; }

    private ChannelConnection() { } // Constructor privado requerido por EF Core.

    public ChannelConnection(
        Guid id,
        Guid tenantId,
        string provider,
        string displayName,
        string credentialsReference,
        Dictionary<string, string>? configurationMetadata = null,
        bool enabled = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de conexión no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("El proveedor es obligatorio.", nameof(provider));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("El nombre visible es obligatorio.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(credentialsReference)) throw new ArgumentException("La referencia de credenciales es obligatoria.", nameof(credentialsReference));

        Id = id;
        TenantId = tenantId;
        Provider = provider.Trim();
        DisplayName = displayName.Trim();
        CredentialsReference = credentialsReference.Trim();
        ConfigurationMetadata = configurationMetadata ?? new Dictionary<string, string>();
        Enabled = enabled;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Enable()
    {
        Enabled = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Disable()
    {
        Enabled = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateConfiguration(string displayName, string credentialsReference, Dictionary<string, string>? configurationMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("El nombre visible es obligatorio.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(credentialsReference)) throw new ArgumentException("La referencia de credenciales es obligatoria.", nameof(credentialsReference));

        DisplayName = displayName.Trim();
        CredentialsReference = credentialsReference.Trim();
        if (configurationMetadata != null)
        {
            ConfigurationMetadata = configurationMetadata;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
