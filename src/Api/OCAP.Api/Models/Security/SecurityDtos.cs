namespace OCAP.Api.Models.Security;

// Petición de autenticación de usuario.
public record LoginRequestDto(string Email, string Password);

// Respuesta exitosa de autenticación con JWT. El refresh token no va en el cuerpo: viaja en la cookie HttpOnly
// (RefreshTokenCookie). Con MfaRequired, AccessToken va vacío: el cliente pide el código y llama a /api/auth/mfa/verify.
public record LoginResponseDto(
    string AccessToken,
    Guid UserId,
    Guid TenantId,
    string Email,
    string RoleName,
    bool MfaRequired = false,
    string? MfaToken = null);

// Definir la contraseña con el enlace de un solo uso.
public record PasswordSetupRequestDto(string Token, string NewPassword);

public record ForgotPasswordRequestDto(string Email);

// Segundo paso del login con MFA.
public record CompleteMfaLoginRequestDto(string MfaToken, string Code);

// DTO de usuario del tenant.
public record UserDto(Guid Id, Guid TenantId, string Email, string FullName, bool IsActive, DateTime CreatedAtUtc);

// DTO de rol RBAC.
public record RoleDto(Guid Id, Guid TenantId, string Name, string Description, List<string> Permissions);

// DTO de permiso granular.
public record PermissionDto(Guid Id, string Code, string Name, string Category, string Description);

// DTO de tenant u organización.
public record TenantDto(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAtUtc);

// Petición de creación de tenant.
public record CreateTenantRequestDto(string Name, string Slug);

// DTO de clave de API.
public record ApiKeyDto(Guid Id, Guid TenantId, Guid UserId, string Prefix, string Name, DateTime ExpiresAtUtc, bool IsRevoked, DateTime? LastUsedAtUtc);

// Respuesta al crear una nueva API key (único momento en que se entrega la clave cruda).
public record CreateApiKeyResponseDto(string RawApiKey, Guid ApiKeyId, string Prefix, string Name, DateTime ExpiresAtUtc);

// DTO de sesión activa de usuario.
public record UserSessionDto(Guid Id, Guid UserId, Guid TenantId, string IpAddress, string UserAgent, DateTime LoginAtUtc, bool IsActive);
