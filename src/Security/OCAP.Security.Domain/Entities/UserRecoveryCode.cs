namespace OCAP.Security.Domain.Entities;

// Entidad para almacenar códigos de recuperación de uso único (Hashed) para MFA (CAP-17).
public class UserRecoveryCode
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public bool IsUsed { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private UserRecoveryCode() { }

    public UserRecoveryCode(Guid id, Guid tenantId, Guid userId, string codeHash, string salt)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        CodeHash = codeHash;
        Salt = salt;
        IsUsed = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAtUtc = DateTime.UtcNow;
    }
}
