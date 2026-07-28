namespace OCAP.Security.Domain.Entities;

// Entidad de dominio que representa una identidad externa vinculada a un usuario de OCAP.
// Permite mapear canales externos (Telegram, WhatsApp, Slack, Discord, etc.) a un único UserId global por Tenant.
public class ExternalIdentity
{
    // Identificador único de la identidad externa.
    public Guid Id { get; private set; }

    // Identificador del tenant al que pertenece esta vinculación de identidad.
    public Guid TenantId { get; private set; }

    // Identificador del usuario global OCAP al que está asociada la identidad.
    public Guid UserId { get; private set; }

    // Proveedor o canal externo (ej. "Telegram", "WhatsApp", "WebChat", "Slack", "MicrosoftTeams").
    public string Provider { get; private set; } = string.Empty;

    // Identificador único del usuario dentro del canal o proveedor externo (ej. ID de chat de Telegram).
    public string ExternalId { get; private set; } = string.Empty;

    // Metadatos adicionales opcionales provistos por la plataforma externa.
    public Dictionary<string, string> Metadata { get; private set; } = new();

    // Marca de tiempo UTC de creación del registro.
    public DateTime CreatedAtUtc { get; private set; }

    private ExternalIdentity() { } // Constructor requerido por el ORM.

    // Constructor de dominio con validación de invariantes.
    public ExternalIdentity(Guid id, Guid tenantId, Guid userId, string provider, string externalId, Dictionary<string, string>? metadata = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de identidad externa no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("El proveedor (Provider) es obligatorio.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalId)) throw new ArgumentException("El ExternalId es obligatorio.", nameof(externalId));

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Provider = provider.Trim();
        ExternalId = externalId.Trim();
        Metadata = metadata ?? new Dictionary<string, string>();
        CreatedAtUtc = DateTime.UtcNow;
    }
}
