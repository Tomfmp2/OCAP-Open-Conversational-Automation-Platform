using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OCAP.Api.Controllers;

/// <summary>
/// Servidor OAuth2 / OpenID Connect (OpenIddict): Authorization Code + PKCE, refresh y client credentials.
/// </summary>
[ApiController]
[AllowAnonymous]
public class ConnectController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IConsentService _consentService;
    private readonly ISecurityAuditService _auditService;
    private readonly OCAPDbContext _dbContext;

    public ConnectController(
        IOpenIddictApplicationManager applicationManager,
        IIdentityService identityService,
        IRefreshTokenService refreshTokenService,
        IConsentService consentService,
        ISecurityAuditService auditService,
        OCAPDbContext dbContext)
    {
        _applicationManager = applicationManager ?? throw new ArgumentNullException(nameof(applicationManager));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _consentService = consentService ?? throw new ArgumentNullException(nameof(consentService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>Endpoint de autorización (code + PKCE). Requiere usuario autenticado vía JWT Bearer.</summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction?.Request
            ?? throw new InvalidOperationException("La solicitud OpenIddict no se pudo recuperar del contexto HTTP.");

        if (!request.HasResponseType(ResponseTypes.Code))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", "Tipo de respuesta no soportado", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.UnsupportedResponseType,
                ErrorDescription = "Solo el response_type=code está soportado."
            });
        }

        var clientId = request.ClientId;
        if (string.IsNullOrEmpty(clientId))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", "El client_id es requerido", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidClient,
                ErrorDescription = "El id de cliente es requerido."
            });
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null)
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"Cliente no encontrado: '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidClient,
                ErrorDescription = "El cliente especificado no existe."
            });
        }

        if (!string.IsNullOrEmpty(request.RedirectUri)
            && !await _applicationManager.ValidateRedirectUriAsync(application, request.RedirectUri, cancellationToken))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"URI de redirección no válida: '{request.RedirectUri}' para cliente '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidRequest,
                ErrorDescription = "La URI de redirección no es válida para este cliente."
            });
        }

        if (string.IsNullOrEmpty(request.CodeChallenge))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"PKCE code_challenge faltante para cliente '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidRequest,
                ErrorDescription = "Se requiere el parámetro PKCE code_challenge."
            });
        }

        if (!string.Equals(request.CodeChallengeMethod, CodeChallengeMethods.Sha256, StringComparison.Ordinal))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"PKCE code_challenge_method no soportado '{request.CodeChallengeMethod}' para cliente '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidRequest,
                ErrorDescription = "El único método PKCE soportado es S256."
            });
        }

        var principal = await ResolveAuthenticatedPrincipalAsync(cancellationToken);
        if (principal is null)
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", "Usuario no autenticado en /connect/authorize", "ConnectController", false, cancellationToken);
            return Challenge(authenticationSchemes: JwtBearerDefaults.AuthenticationScheme);
        }

        if (!TryGetUserAndTenantIds(principal, out var userId, out var tenantId))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El token de autenticación no contiene subject/tenant_id válidos."
            });
        }

        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && u.IsActive, cancellationToken);
        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);

        if (user is null || tenant is null)
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "Authorization.Denied", "Usuario o tenant inactivo/inexistente", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El usuario autenticado o su tenant no están disponibles."
            });
        }

        var scopes = request.GetScopes();
        await _consentService.GrantConsentAsync(tenantId, userId, clientId, scopes, cancellationToken);

        var identity = await BuildUserIdentityAsync(user.Id, tenant.Id, tenant.Slug, user.Email, user.FullName, request.Nonce, cancellationToken);
        var oidcPrincipal = new ClaimsPrincipal(identity);
        oidcPrincipal.SetScopes(scopes);

        await _auditService.LogSecurityEventAsync(
            tenantId, userId, "Authorization.Granted", $"Código de autorización emitido para cliente '{clientId}'", "ConnectController", true, cancellationToken);

        return SignIn(oidcPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Canje de tokens: authorization_code, refresh_token o client_credentials.</summary>
    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction?.Request
            ?? throw new InvalidOperationException("La solicitud OpenIddict no se pudo recuperar del contexto HTTP.");

        if (request.IsClientCredentialsGrantType())
        {
            return await HandleClientCredentialsAsync(request, cancellationToken);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenAsync(request, cancellationToken);
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            return await HandleAuthorizationCodeAsync(request, cancellationToken);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "Grant type no soportado."
        });
    }

    private async Task<IActionResult> HandleClientCredentialsAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId;
        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidClient,
                ErrorDescription = "El id de cliente es requerido."
            });
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidClient,
                ErrorDescription = "Cliente desconocido."
            });
        }

        // Client credentials: el subject es el propio cliente (sin suplantar usuarios humanos).
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        AddClaimWithDestinations(identity, Claims.Subject, clientId);
        AddClaimWithDestinations(identity, "client_id", clientId);
        AddClaimWithDestinations(identity, "token_usage", "client_credentials");
        AddClaimWithDestinations(identity, "auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        var properties = await _applicationManager.GetPropertiesAsync(application, cancellationToken);
        if (properties.TryGetValue("tenant_id", out var tenantProp)
            && Guid.TryParse(tenantProp.GetString(), out var tenantId)
            && tenantId != Guid.Empty)
        {
            var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
            if (tenant is null)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.InvalidClient,
                    ErrorDescription = "El tenant asociado al cliente no existe o está inactivo."
                });
            }

            AddClaimWithDestinations(identity, "tenant_id", tenant.Id.ToString());
            AddClaimWithDestinations(identity, "tenant_slug", tenant.Slug);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "OAuth.ClientCredentialsTokenIssued",
            $"Token de cliente emitido para {clientId}", "ConnectController", true, cancellationToken);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var userIdString = result.Principal?.FindFirst(Claims.Subject)?.Value;
        var tenantIdString = result.Principal?.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El token de refresco no es válido o ha expirado."
            });
        }

        if (string.IsNullOrEmpty(tenantIdString) || !Guid.TryParse(tenantIdString, out var tenantId) || tenantId == Guid.Empty)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El token de refresco no contiene un tenant válido."
            });
        }

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
        var user = await _dbContext.UserIdentities.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (tenant is null || user is null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "Usuario o tenant asociados al refresh token no existen."
            });
        }

        var identity = await BuildUserIdentityAsync(user.Id, tenant.Id, tenant.Slug, user.Email, user.FullName, null, cancellationToken);
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        await _auditService.LogSecurityEventAsync(tenantId, userId, "OAuth.RefreshTokenIssued", "Token de acceso refrescado", "ConnectController", true, cancellationToken);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleAuthorizationCodeAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            await _auditService.LogSecurityEventAsync(
                Guid.Empty, Guid.Empty, "Authorization.Denied", "Canje de código de autorización fallido o expirado", "ConnectController", false, cancellationToken);

            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El código de autorización no es válido, ha expirado o ya fue utilizado."
            });
        }

        var userIdString = result.Principal.FindFirst(Claims.Subject)?.Value;
        var tenantIdString = result.Principal.FindFirst("tenant_id")?.Value;
        var nonce = result.Principal.FindFirst(Claims.Nonce)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El sujeto del token no es válido."
            });
        }

        if (string.IsNullOrEmpty(tenantIdString) || !Guid.TryParse(tenantIdString, out var tenantId) || tenantId == Guid.Empty)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "El código de autorización no contiene un tenant válido."
            });
        }

        var tenant = await _dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
        var user = await _dbContext.UserIdentities.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (tenant is null || user is null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "Usuario o tenant asociados al código de autorización no existen."
            });
        }

        var identity = await BuildUserIdentityAsync(user.Id, tenant.Id, tenant.Slug, user.Email, user.FullName, nonce, cancellationToken);
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        await _auditService.LogSecurityEventAsync(tenantId, userId, "OAuth.AuthorizationCodeExchanged", "Código canjeado por tokens", "ConnectController", true, cancellationToken);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal?> ResolveAuthenticatedPrincipalAsync(CancellationToken cancellationToken)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return User;
        }

        var bearer = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (bearer.Succeeded && bearer.Principal?.Identity?.IsAuthenticated == true)
        {
            return bearer.Principal;
        }

        return null;
    }

    private static bool TryGetUserAndTenantIds(ClaimsPrincipal principal, out Guid userId, out Guid tenantId)
    {
        userId = Guid.Empty;
        tenantId = Guid.Empty;

        var userClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value
                        ?? principal.FindFirst("user_id")?.Value;
        var tenantClaim = principal.FindFirst("tenant_id")?.Value
                          ?? principal.FindFirst("TenantId")?.Value;

        return !string.IsNullOrWhiteSpace(userClaim)
               && Guid.TryParse(userClaim, out userId)
               && userId != Guid.Empty
               && !string.IsNullOrWhiteSpace(tenantClaim)
               && Guid.TryParse(tenantClaim, out tenantId)
               && tenantId != Guid.Empty;
    }

    private async Task<ClaimsIdentity> BuildUserIdentityAsync(
        Guid userId,
        Guid tenantId,
        string tenantSlug,
        string email,
        string fullName,
        string? nonce,
        CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        AddClaimWithDestinations(identity, Claims.Subject, userId.ToString());
        AddClaimWithDestinations(identity, Claims.Email, email);
        AddClaimWithDestinations(identity, Claims.Name, fullName);
        AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
        AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
        AddClaimWithDestinations(identity, "user_id", userId.ToString());
        AddClaimWithDestinations(identity, "auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        if (!string.IsNullOrEmpty(nonce))
        {
            AddClaimWithDestinations(identity, Claims.Nonce, nonce);
        }

        var roles = await _identityService.GetUserRolesAsync(userId, tenantId, cancellationToken);
        var permissions = await _identityService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

        foreach (var role in roles)
        {
            AddClaimWithDestinations(identity, "roles", role.Name);
            AddClaimWithDestinations(identity, ClaimTypes.Role, role.Name);
        }

        foreach (var perm in permissions)
        {
            AddClaimWithDestinations(identity, "permissions", perm);
        }

        return identity;
    }

    private static void AddClaimWithDestinations(ClaimsIdentity identity, string type, string value)
    {
        var claim = new Claim(type, value);
        claim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        identity.AddClaim(claim);
    }
}
