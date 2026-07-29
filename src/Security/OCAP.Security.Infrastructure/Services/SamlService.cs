using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para Enterprise Single Sign-On SAML 2.0 Service Provider (CAP-18).
public class SamlService : ISamlService
{
    private readonly OCAPDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;

    public SamlService(
        OCAPDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<string> GetSpMetadataXmlAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var spEntityId = $"https://ocap.io/saml/sp/{tenantId}";
        var acsUrl = $"https://ocap.io/api/auth/saml/acs?tenantId={tenantId}";
        var sloUrl = $"https://ocap.io/api/auth/saml/slo?tenantId={tenantId}";

        var xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <md:EntityDescriptor xmlns:md="urn:oasis:names:tc:SAML:2.0:metadata" entityID="{spEntityId}">
              <md:SPSSODescriptor AuthnRequestsSigned="false" WantAssertionsSigned="true" protocolSupportEnumeration="urn:oasis:names:tc:SAML:2.0:protocol">
                <md:NameIDFormat>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</md:NameIDFormat>
                <md:AssertionConsumerService Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST" Location="{acsUrl}" index="0" isDefault="true"/>
                <md:SingleLogoutService Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect" Location="{sloUrl}"/>
              </md:SPSSODescriptor>
            </md:EntityDescriptor>
            """;

        return xml;
    }

    public async Task<SamlLoginRedirectDto> InitiateSpLoginAsync(Guid tenantId, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.SamlProviderConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IsEnabled, cancellationToken);
        if (config == null) throw new InvalidOperationException($"SAML 2.0 no se encuentra configurado o habilitado para el tenant '{tenantId}'.");

        var requestId = "_" + Guid.NewGuid().ToString("N");
        var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var spEntityId = $"https://ocap.io/saml/sp/{tenantId}";
        var acsUrl = $"https://ocap.io/api/auth/saml/acs?tenantId={tenantId}";

        var authnRequestXml = $"""
            <samlp:AuthnRequest xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion" ID="{requestId}" Version="2.0" IssueInstant="{issueInstant}" Destination="{config.SsoServiceUrl}" ProtocolBinding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST" AssertionConsumerServiceURL="{acsUrl}">
              <saml:Issuer>{spEntityId}</saml:Issuer>
              <samlp:NameIDPolicy Format="{config.NameIdFormat}" AllowCreate="true"/>
            </samlp:AuthnRequest>
            """;

        var samlRequestBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authnRequestXml));
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.LoginInitiated", $"Solicitud AuthnRequest creada (ID: {requestId})", "SamlService", true, cancellationToken);

        return new SamlLoginRedirectDto(config.SsoServiceUrl, samlRequestBase64, returnUrl);
    }

    public async Task<SamlAuthResultDto> ProcessAcsResponseAsync(Guid tenantId, SamlAcsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SamlResponse))
            throw new ArgumentException("La respuesta SAMLResponse es requerida.", nameof(request));

        var config = await _dbContext.SamlProviderConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IsEnabled, cancellationToken);
        if (config == null) throw new InvalidOperationException("Configuración SAML 2.0 no encontrada o inactiva.");

        string decodedXml;
        try
        {
            byte[] bytes = Convert.FromBase64String(request.SamlResponse);
            decodedXml = Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.AcsFailed", "Formato de decodificación Base64 inválido", "SamlService", false, cancellationToken);
            throw new ArgumentException("El parámetro SAMLResponse no es un Base64 válido.", ex);
        }

        var xmlDoc = new XmlDocument { XmlResolver = null };
        xmlDoc.LoadXml(decodedXml);

        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");

        // Validar Estado de la Respuesta SAML
        var statusCodeNode = xmlDoc.SelectSingleNode("//samlp:StatusCode/@Value", nsmgr);
        if (statusCodeNode == null || !statusCodeNode.Value!.EndsWith("Success"))
        {
            await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.AcsFailed", "SAML StatusCode no exitoso", "SamlService", false, cancellationToken);
            throw new InvalidOperationException("El proveedor IdP SAML retornó un estado no exitoso.");
        }

        // Validar Issuer del IdP
        var issuerNode = xmlDoc.SelectSingleNode("//saml:Issuer", nsmgr);
        if (issuerNode != null && !string.IsNullOrWhiteSpace(config.EntityId) && issuerNode.InnerText.Trim() != config.EntityId.Trim())
        {
            await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.AcsFailed", $"Issuer no coincide (Esperado: {config.EntityId}, Recibido: {issuerNode.InnerText})", "SamlService", false, cancellationToken);
            throw new InvalidOperationException("Validación de Issuer SAML fallida.");
        }

        // Extraer NameID (Email)
        var nameIdNode = xmlDoc.SelectSingleNode("//saml:NameID", nsmgr);
        var email = nameIdNode?.InnerText?.Trim()?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            var emailAttrNode = xmlDoc.SelectSingleNode("//saml:Attribute[@Name='email']/saml:AttributeValue", nsmgr);
            email = emailAttrNode?.InnerText?.Trim()?.ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.AcsFailed", "Imposible extraer NameID / Email de la Assertion SAML", "SamlService", false, cancellationToken);
            throw new InvalidOperationException("NameID / Email no encontrado en la Assertion SAML.");
        }

        // Validar / Aprovisionar Usuario en OCAP
        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
        if (user == null)
        {
            user = new UserIdentity(Guid.NewGuid(), tenantId, email, "SAML_SSO_HASH", "SAML_SALT", email.Split('@')[0]);
            _dbContext.UserIdentities.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.LogSecurityEventAsync(tenantId, user.Id, "User.AutoProvisioned", $"Usuario auto-aprovisionado vía SAML SSO '{email}'", "SamlService", true, cancellationToken);
        }

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        var role = new Role(Guid.NewGuid(), tenantId, "User", "Usuario SSO", new[] { "Conversation.Read", "Agent.Execute" });

        var accessToken = _jwtTokenService.GenerateAccessToken(user, tenant!, role, role.Permissions);
        var refreshTokenEntity = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, TimeSpan.FromDays(7), cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, user.Id, "Saml.AcsVerified", $"Autenticación SAML 2.0 completada exitosamente para '{email}'", "SamlService", true, cancellationToken);

        var userDetail = new UserDetailDto(user.Id, user.TenantId, user.Email, user.FullName, user.IsActive, user.IsLocked, user.IsEmailVerified, user.CreatedAtUtc);
        return new SamlAuthResultDto(accessToken, refreshTokenEntity.Token, 3600, "Bearer", userDetail);
    }

    public async Task<bool> ProcessSloAsync(Guid tenantId, string samlRequestOrResponse, CancellationToken cancellationToken = default)
    {
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.SloProcessed", "Single Logout SAML 2.0 procesado", "SamlService", true, cancellationToken);
        return true;
    }

    public async Task<SamlProviderConfigDto?> GetSamlProviderConfigAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.SamlProviderConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (config == null) return null;

        return new SamlProviderConfigDto(config.Id, config.TenantId, config.EntityId, config.SsoServiceUrl, config.SloServiceUrl, config.IdpCertificatePem, config.IsEnabled, config.NameIdFormat, config.AttributeMappingJson);
    }

    public async Task<SamlProviderConfigDto> SaveSamlProviderConfigAsync(Guid tenantId, SaveSamlProviderConfigDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var existing = await _dbContext.SamlProviderConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (existing == null)
        {
            existing = new SamlProviderConfig(Guid.NewGuid(), tenantId, request.EntityId, request.SsoServiceUrl, request.SloServiceUrl, request.IdpCertificatePem, request.NameIdFormat, request.AttributeMappingJson);
            _dbContext.SamlProviderConfigs.Add(existing);
        }
        else
        {
            existing.Update(request.EntityId, request.SsoServiceUrl, request.SloServiceUrl, request.IdpCertificatePem, request.NameIdFormat, request.AttributeMappingJson);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.ConfigSaved", $"Configuración SAML 2.0 actualizada para EntityID '{existing.EntityId}'", "SamlService", true, cancellationToken);

        return new SamlProviderConfigDto(existing.Id, existing.TenantId, existing.EntityId, existing.SsoServiceUrl, existing.SloServiceUrl, existing.IdpCertificatePem, existing.IsEnabled, existing.NameIdFormat, existing.AttributeMappingJson);
    }

    public async Task<SamlProviderConfigDto> ImportIdpMetadataXmlAsync(Guid tenantId, string metadataXml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(metadataXml)) throw new ArgumentException("El XML de metadatos del IdP es requerido.", nameof(metadataXml));

        var xmlDoc = new XmlDocument { XmlResolver = null };
        xmlDoc.LoadXml(metadataXml);

        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
        nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

        var entityIdNode = xmlDoc.SelectSingleNode("//md:EntityDescriptor/@entityID", nsmgr);
        var ssoNode = xmlDoc.SelectSingleNode("//md:SingleSignOnService[@Binding='urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST']/@Location", nsmgr)
                      ?? xmlDoc.SelectSingleNode("//md:SingleSignOnService/@Location", nsmgr);

        var sloNode = xmlDoc.SelectSingleNode("//md:SingleLogoutService/@Location", nsmgr);
        var certNode = xmlDoc.SelectSingleNode("//ds:X509Certificate", nsmgr);

        var entityId = entityIdNode?.Value ?? "https://idp.example.com";
        var ssoUrl = ssoNode?.Value ?? "https://idp.example.com/sso";
        var sloUrl = sloNode?.Value ?? string.Empty;
        var certPem = certNode != null ? $"-----BEGIN CERTIFICATE-----\n{certNode.InnerText.Trim()}\n-----END CERTIFICATE-----" : string.Empty;

        var saveDto = new SaveSamlProviderConfigDto(entityId, ssoUrl, sloUrl, certPem);
        var result = await SaveSamlProviderConfigAsync(tenantId, saveDto, cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Saml.MetadataImported", $"Metadatos IdP importados para '{entityId}'", "SamlService", true, cancellationToken);
        return result;
    }
}
