namespace OCAP.Security.Domain.Entities;

// Entidad de Refresh Token para rotación segura de JWTs sin requerir re-autenticación de usuario.
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private RefreshToken() { } // Constructor ORM.

    public RefreshToken(Guid id, Guid userId, string token, DateTime expiresAtUtc, Guid tenantId = default)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        IsRevoked = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    // Revoca el token impidiendo su reutilización.
    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
    }

    // Verifica si el token ha expirado.
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    // Indica si el token es válido para consumo.
    public bool IsActive => !IsRevoked && !IsExpired;
}
