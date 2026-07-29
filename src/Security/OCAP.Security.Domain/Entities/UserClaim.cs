namespace OCAP.Security.Domain.Entities;

// Entidad que representa un Claim de identidad asociado a un usuario y tenant para OpenIddict/JWT.
public class UserClaim
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string ClaimType { get; private set; } = string.Empty;
    public string ClaimValue { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private UserClaim() { } // ORM

    public UserClaim(Guid id, Guid userId, Guid tenantId, string claimType, string claimValue)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede ser vacío.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(claimType)) throw new ArgumentException("El ClaimType es requerido.", nameof(claimType));

        Id = id;
        UserId = userId;
        TenantId = tenantId;
        ClaimType = claimType.Trim();
        ClaimValue = claimValue?.Trim() ?? string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateValue(string newValue)
    {
        ClaimValue = newValue?.Trim() ?? string.Empty;
    }
}
