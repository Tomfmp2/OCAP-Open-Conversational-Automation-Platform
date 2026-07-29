namespace OCAP.Security.Abstractions.DTOs;

// DTOs para MFA (TOTP / Recovery Codes) y WebAuthn / Passkeys (CAP-17).

public record MfaSetupDto(
    string Secret,
    string QrCodeUri
);

public record EnableMfaRequestDto(
    string Code
);

public record EnableMfaResponseDto(
    IReadOnlyList<string> RecoveryCodes
);

public record VerifyMfaRequestDto(
    string Code
);

public record WebAuthnDeviceDto(
    Guid Id,
    string CredentialId,
    string DeviceName,
    DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc
);

public record WebAuthnRegisterOptionsDto(
    string Challenge,
    string RpName,
    string RpId,
    string UserId,
    string UserName
);

public record WebAuthnRegisterRequestDto(
    string DeviceName,
    string CredentialId,
    string PublicKeyPem
);

public record WebAuthnAssertionOptionsDto(
    string Challenge,
    IReadOnlyList<string> AllowedCredentialIds
);

public record WebAuthnAssertionRequestDto(
    string CredentialId,
    string AuthenticatorData,
    string ClientDataJson,
    string Signature
);
