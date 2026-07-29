using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato de servicio para Enterprise Single Sign-On mediante SAML 2.0 (CAP-18).
public interface ISamlService
{
    Task<string> GetSpMetadataXmlAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SamlLoginRedirectDto> InitiateSpLoginAsync(Guid tenantId, string? returnUrl = null, CancellationToken cancellationToken = default);
    Task<SamlAuthResultDto> ProcessAcsResponseAsync(Guid tenantId, SamlAcsRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ProcessSloAsync(Guid tenantId, string samlRequestOrResponse, CancellationToken cancellationToken = default);
    Task<SamlProviderConfigDto?> GetSamlProviderConfigAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SamlProviderConfigDto> SaveSamlProviderConfigAsync(Guid tenantId, SaveSamlProviderConfigDto request, CancellationToken cancellationToken = default);
    Task<SamlProviderConfigDto> ImportIdpMetadataXmlAsync(Guid tenantId, string metadataXml, CancellationToken cancellationToken = default);
}
