namespace OCAP.Security.Domain.Entities;

// Entidad que representa una clave de API segura con almacenamiento en Hash (X-API-Key).
public class ApiKey
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;
    public string Prefix { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ApiKey() { } // Constructor ORM.

    public ApiKey(Guid id, Guid tenantId, Guid userId, string keyHash, string prefix, string name, DateTime expiresAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        KeyHash = keyHash;
        Prefix = prefix;
        Name = name;
        ExpiresAtUtc = expiresAtUtc;
        IsRevoked = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    // Registra el uso de la clave de API actualizando la fecha del último acceso.
    public void RecordUsage() => LastUsedAtUtc = DateTime.UtcNow;

    // Revoca la clave de API.
    public void Revoke() => IsRevoked = true;

    // Indica si la clave es activa y válida.
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;
}
