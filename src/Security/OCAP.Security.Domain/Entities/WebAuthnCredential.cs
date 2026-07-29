namespace OCAP.Security.Domain.Entities;

// Entidad de credencial WebAuthn / FIDO2 para inicio de sesión mediante Passkeys (CAP-17).
public class WebAuthnCredential
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string CredentialId { get; private set; } = string.Empty;
    public string PublicKeyPem { get; private set; } = string.Empty;
    public uint SignCount { get; private set; }
    public string DeviceName { get; private set; } = string.Empty;
    public string? Aaguid { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }

    private WebAuthnCredential() { }

    public WebAuthnCredential(Guid id, Guid tenantId, Guid userId, string credentialId, string publicKeyPem, string deviceName, string? aaguid = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de credencial no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));
        if (string.IsNullOrWhiteSpace(credentialId)) throw new ArgumentException("El CredentialId es obligatorio.", nameof(credentialId));

        Id = id;
        TenantId = tenantId;
        UserId = userId;
        CredentialId = credentialId;
        PublicKeyPem = publicKeyPem ?? string.Empty;
        DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Passkey Device" : deviceName.Trim();
        Aaguid = aaguid;
        SignCount = 0;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateSignCount(uint newSignCount)
    {
        if (newSignCount <= SignCount && SignCount > 0)
        {
            throw new InvalidOperationException("Posible clonación o ataque de Replay detectado en la credencial WebAuthn.");
        }
        SignCount = newSignCount;
        LastUsedAtUtc = DateTime.UtcNow;
    }
}
