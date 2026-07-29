namespace OCAP.Security.Abstractions.DTOs;

// DTO que representa la información pública de un proveedor de autenticación externo habilitado (CAP-15).
public record ExternalProviderInfoDto(
    string Name,
    string DisplayName,
    bool IsEnabled,
    string? IconUrl
);

// DTO con la carga útil estandarizada de usuario obtenida de un proveedor externo tras autenticación OAuth2/OIDC.
public record ExternalUserPayloadDto(
    string Provider,
    string ExternalId,
    string Email,
    string FullName,
    string? PictureUrl,
    IReadOnlyDictionary<string, string>? Claims
);

// DTO de resultado del desafío de autenticación externa (URL de redirección al proveedor).
public record ExternalAuthChallengeDto(
    string Provider,
    string AuthorizationUrl,
    string State
);

// DTO de solicitud de callback tras redirigir del proveedor externo.
public record ExternalAuthCallbackRequestDto(
    string Provider,
    string Code,
    string State,
    string? RedirectUri = null
);

// DTO de resultado de inicio de sesión o vinculación externa.
public record ExternalAuthLoginResultDto(
    bool IsSuccess,
    string? AccessToken,
    string? RefreshToken,
    Guid? UserId,
    Guid? TenantId,
    string? UserEmail,
    string? UserName,
    string? ErrorMessage
);

// DTO de configuración de proveedor externo en appsettings.
public class ExternalProviderSettings
{
    public bool IsEnabled { get; set; } = false;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Authority { get; set; }
    public string? Scope { get; set; }
    public string? TenantId { get; set; }
}

public class ExternalAuthenticationSettings
{
    public bool AutoProvisionUsers { get; set; } = true;
    public Dictionary<string, ExternalProviderSettings> ExternalProviders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
