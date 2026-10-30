using System.Security.Cryptography;
using System.Text;

namespace OCAP.Security.Domain.Entities;

// Refresh Token para rotar el JWT sin volver a pedir la contraseña. La base solo guarda el SHA-256 del token: una
// copia de la base no permite abrir sesiones.
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;

    // Todos los tokens de una misma sesión comparten familia: si se presenta uno ya rotado, se revoca la sesión entera.
    public Guid FamilyId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private RefreshToken() { } // Constructor ORM.

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc, Guid tenantId, Guid familyId)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId == Guid.Empty ? id : familyId;
        ExpiresAtUtc = expiresAtUtc;
        IsRevoked = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    // Revoca el token impidiendo su reutilización.
    public void Revoke(string? replacedByTokenHash = null)
    {
        IsRevoked = true;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsActive => !IsRevoked && !IsExpired;

    // Revocado por una rotación (no por logout): volver a verlo significa que alguien más tiene una copia.
    public bool WasRotated => IsRevoked && ReplacedByTokenHash is not null;
}
