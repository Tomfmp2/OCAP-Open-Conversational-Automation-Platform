namespace OCAP.Security.Abstractions.DTOs;

// DTOs para la configuración, inicio de sesión y callbacks SAML 2.0 (CAP-18).

public record SamlProviderConfigDto(
    Guid Id,
    Guid TenantId,
    string EntityId,
    string SsoServiceUrl,
    string SloServiceUrl,
    string IdpCertificatePem,
    bool IsEnabled,
    string NameIdFormat,
    string AttributeMappingJson
);

public record SaveSamlProviderConfigDto(
    string EntityId,
    string SsoServiceUrl,
    string? SloServiceUrl = null,
    string? IdpCertificatePem = null,
    string? NameIdFormat = null,
    string? AttributeMappingJson = null
);

public record SamlLoginRedirectDto(
    string SsoUrl,
    string SamlRequestBase64,
    string? RelayState
);

public record SamlAcsRequestDto(
    string SamlResponse,
    string? RelayState = null
);

public record SamlAuthResultDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserDetailDto User
);

public record ImportMetadataRequestDto(
    string MetadataXml
);
