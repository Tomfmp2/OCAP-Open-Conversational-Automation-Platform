using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato de servicio para autenticación WebAuthn / FIDO2 / Passkeys (CAP-17).
public interface IWebAuthnService
{
    Task<WebAuthnRegisterOptionsDto> GenerateRegistrationOptionsAsync(Guid tenantId, Guid userId, string userEmail, CancellationToken cancellationToken = default);
    Task<WebAuthnDeviceDto> CompleteRegistrationAsync(Guid tenantId, Guid userId, WebAuthnRegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<WebAuthnAssertionOptionsDto> GenerateAssertionOptionsAsync(Guid tenantId, string userEmail, CancellationToken cancellationToken = default);
    Task<bool> CompleteAssertionAsync(Guid tenantId, WebAuthnAssertionRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebAuthnDeviceDto>> GetRegisteredDevicesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteDeviceAsync(Guid tenantId, Guid userId, Guid credentialDbId, CancellationToken cancellationToken = default);
}
