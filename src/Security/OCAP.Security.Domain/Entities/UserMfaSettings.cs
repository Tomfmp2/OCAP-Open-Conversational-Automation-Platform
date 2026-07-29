namespace OCAP.Security.Domain.Entities;

// Entidad de configuración de Autenticación de Múltiples Factores (TOTP / RFC 6238) por usuario (CAP-17).
public class UserMfaSettings
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsEnabled { get; private set; }
    public string EncryptedTotpSecret { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? EnabledAtUtc { get; private set; }

    private UserMfaSettings() { }

    public UserMfaSettings(Guid id, Guid tenantId, Guid userId, string encryptedTotpSecret)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de MFA no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));
        if (string.IsNullOrWhiteSpace(encryptedTotpSecret)) throw new ArgumentException("El secreto TOTP es obligatorio.", nameof(encryptedTotpSecret));

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        EncryptedTotpSecret = encryptedTotpSecret;
        IsEnabled = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        EnabledAtUtc = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        EnabledAtUtc = null;
    }

    public void UpdateSecret(string newEncryptedSecret)
    {
        if (string.IsNullOrWhiteSpace(newEncryptedSecret)) throw new ArgumentException("El secreto no puede ser vacío.", nameof(newEncryptedSecret));
        EncryptedTotpSecret = newEncryptedSecret;
        IsEnabled = false;
        EnabledAtUtc = null;
    }
}
