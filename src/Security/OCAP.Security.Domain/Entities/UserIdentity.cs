namespace OCAP.Security.Domain.Entities;

// Entidad central de identidad de usuario con soporte multi-tenant, gestión administrativa y autenticación segura (CAP-16).
public class UserIdentity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsLocked { get; private set; } = false;
    public DateTime? LockoutEndUtc { get; private set; }
    public bool IsEmailVerified { get; private set; } = false;
    public string? PasswordResetToken { get; private set; }
    public string? InviteToken { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private UserIdentity() { } // Constructor privado para el ORM.

    public UserIdentity(Guid id, Guid tenantId, string email, string passwordHash, string salt, string fullName, string? inviteToken = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de usuario no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El ID de tenant no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("El email es requerido.", nameof(email));

        Id = id;
        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Salt = salt;
        FullName = fullName ?? string.Empty;
        IsActive = true;
        IsLocked = false;
        IsEmailVerified = false;
        InviteToken = inviteToken;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash, string newSalt)
    {
        PasswordHash = newPasswordHash;
        Salt = newSalt;
        PasswordResetToken = null;
    }

    public void UpdateProfile(string fullName)
    {
        FullName = fullName ?? string.Empty;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void Lock(TimeSpan? duration = null)
    {
        IsLocked = true;
        LockoutEndUtc = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : DateTime.UtcNow.AddYears(100);
    }

    public void Unlock()
    {
        IsLocked = false;
        LockoutEndUtc = null;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
    }

    public void SetPasswordResetToken(string token)
    {
        PasswordResetToken = token;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
    }
}
