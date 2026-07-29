namespace OCAP.Security.Domain.Entities;

// Entidad que almacena el consentimiento de autorización otorgado por un usuario a una aplicación cliente OAuth2/OIDC (CAP-14).
public class UserConsent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string GrantedScopes { get; private set; } = string.Empty;
    public DateTime ApprovedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    private UserConsent() { } // Constructor privado para EF Core.

    public UserConsent(Guid id, Guid tenantId, Guid userId, string clientId, string grantedScopes, DateTime? expiresAtUtc = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de consentimiento no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("El ClientId no puede ser vacío.", nameof(clientId));

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        ClientId = clientId.Trim();
        GrantedScopes = grantedScopes ?? "*";
        ApprovedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        IsRevoked = false;
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
