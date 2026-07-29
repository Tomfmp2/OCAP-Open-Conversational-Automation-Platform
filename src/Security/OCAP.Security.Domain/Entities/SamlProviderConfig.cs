namespace OCAP.Security.Domain.Entities;

// Entidad de configuración del proveedor SAML 2.0 Identity Provider (IdP) por Tenant (CAP-18).
public class SamlProviderConfig
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EntityId { get; private set; } = string.Empty;
    public string SsoServiceUrl { get; private set; } = string.Empty;
    public string SloServiceUrl { get; private set; } = string.Empty;
    public string IdpCertificatePem { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public string NameIdFormat { get; private set; } = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";
    public string AttributeMappingJson { get; private set; } = "{\"email\":\"email\",\"name\":\"name\",\"role\":\"role\"}";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private SamlProviderConfig() { }

    public SamlProviderConfig(
        Guid id,
        Guid tenantId,
        string entityId,
        string ssoServiceUrl,
        string? sloServiceUrl = null,
        string? idpCertificatePem = null,
        string? nameIdFormat = null,
        string? attributeMappingJson = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("El EntityId es obligatorio.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(ssoServiceUrl)) throw new ArgumentException("El SsoServiceUrl es obligatorio.", nameof(ssoServiceUrl));

        Id = id;
        TenantId = tenantId;
        EntityId = entityId.Trim();
        SsoServiceUrl = ssoServiceUrl.Trim();
        SloServiceUrl = sloServiceUrl?.Trim() ?? string.Empty;
        IdpCertificatePem = idpCertificatePem?.Trim() ?? string.Empty;
        NameIdFormat = string.IsNullOrWhiteSpace(nameIdFormat) ? "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress" : nameIdFormat.Trim();
        AttributeMappingJson = string.IsNullOrWhiteSpace(attributeMappingJson) ? "{\"email\":\"email\",\"name\":\"name\",\"role\":\"role\"}" : attributeMappingJson.Trim();
        IsEnabled = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string entityId, string ssoServiceUrl, string? sloServiceUrl, string? idpCertificatePem, string? nameIdFormat, string? attributeMappingJson)
    {
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("El EntityId es obligatorio.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(ssoServiceUrl)) throw new ArgumentException("El SsoServiceUrl es obligatorio.", nameof(ssoServiceUrl));

        EntityId = entityId.Trim();
        SsoServiceUrl = ssoServiceUrl.Trim();
        SloServiceUrl = sloServiceUrl?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(idpCertificatePem)) IdpCertificatePem = idpCertificatePem.Trim();
        if (!string.IsNullOrWhiteSpace(nameIdFormat)) NameIdFormat = nameIdFormat.Trim();
        if (!string.IsNullOrWhiteSpace(attributeMappingJson)) AttributeMappingJson = attributeMappingJson.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Enable() { IsEnabled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAtUtc = DateTime.UtcNow; }
}
