namespace OCAP.Security.Domain.Entities;

// Entidad de mapeo de IDs y versión ETag entre SCIM 2.0 (ExternalId) y recursos locales (CAP-19).
public class ScimExternalMapping
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ResourceType { get; private set; } = "User"; // User, Group
    public Guid LocalId { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string ETag { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ScimExternalMapping() { }

    public ScimExternalMapping(Guid id, Guid tenantId, string resourceType, Guid localId, string externalId, string? eTag = null)
    {
        Id = id;
        TenantId = tenantId;
        ResourceType = resourceType;
        LocalId = localId;
        ExternalId = externalId;
        ETag = string.IsNullOrWhiteSpace(eTag) ? Guid.NewGuid().ToString("N") : eTag;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateETag(string? newETag = null)
    {
        ETag = string.IsNullOrWhiteSpace(newETag) ? Guid.NewGuid().ToString("N") : newETag;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
