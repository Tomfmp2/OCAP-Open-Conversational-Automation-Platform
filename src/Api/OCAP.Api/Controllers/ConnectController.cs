using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace OCAP.Api.Controllers;

// Controlador para Servidor de Autorización OAuth2 / OpenID Connect (OpenIddict) con soporte de Code Flow y PKCE (CAP-14)
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

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction?.Request
            ?? throw new InvalidOperationException("La solicitud OpenIddict no se pudo recuperar del contexto HTTP.");

        if (!request.HasResponseType(OpenIddictConstants.ResponseTypes.Code))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", "Tipo de respuesta no soportado", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.UnsupportedResponseType,
                ErrorDescription = "Solo el response_type=code está soportado."
            });
        }

        var clientId = request.ClientId;
        if (string.IsNullOrEmpty(clientId))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", "El client_id es requerido", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidClient,
                ErrorDescription = "El id de cliente es requerido."
            });
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null)
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"Cliente no encontrado: '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidClient,
                ErrorDescription = "El cliente especificado no existe."
            });
        }

        if (!string.IsNullOrEmpty(request.RedirectUri))
        {
            if (!await _applicationManager.ValidateRedirectUriAsync(application, request.RedirectUri, cancellationToken))
            {
                await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"URI de redirección no válida: '{request.RedirectUri}' para cliente '{clientId}'", "ConnectController", false, cancellationToken);
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidRequest,
                    ErrorDescription = "La URI de redirección no es válida para este cliente."
                });
            }
        }

        if (string.IsNullOrEmpty(request.CodeChallenge))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"PKCE code_challenge faltante para cliente '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = "Se requiere el parámetro PKCE code_challenge."
            });
        }

        if (!string.Equals(request.CodeChallengeMethod, OpenIddictConstants.CodeChallengeMethods.Sha256, StringComparison.Ordinal))
        {
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "Authorization.Denied", $"PKCE code_challenge_method no soportado '{request.CodeChallengeMethod}' para cliente '{clientId}'", "ConnectController", false, cancellationToken);
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = "El único método PKCE soportado es S256."
            });
        }

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(cancellationToken);
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(cancellationToken);

        var userId = user?.Id ?? Guid.NewGuid();
        var tenantId = tenant?.Id ?? Guid.NewGuid();
        var tenantSlug = tenant?.Slug ?? "default-tenant";
        var userEmail = user?.Email ?? "admin@ocap.io";
        var userName = user?.FullName ?? "Administrador OCAP";

        var scopes = request.GetScopes();
        await _consentService.GrantConsentAsync(tenantId, userId, clientId, scopes, cancellationToken);

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Subject, userId.ToString());
        AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Email, userEmail);
        AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Name, userName);
        AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
        AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
        AddClaimWithDestinations(identity, "user_id", userId.ToString());
        AddClaimWithDestinations(identity, "auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        if (!string.IsNullOrEmpty(request.Nonce))
        {
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Nonce, request.Nonce);
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

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        await _auditService.LogSecurityEventAsync(
            tenantId, userId, "Authorization.Granted", $"Código de autorización emitido exitosamente para el cliente '{clientId}'", "ConnectController", true, cancellationToken);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction?.Request
            ?? throw new InvalidOperationException("La solicitud OpenIddict no se pudo recuperar del contexto HTTP.");

        if (request.IsClientCredentialsGrantType())
        {
            var clientId = request.ClientId;
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidClient,
                    ErrorDescription = "El id de cliente es requerido."
                });
            }

            var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(cancellationToken);
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(cancellationToken);

            var userId = user?.Id ?? Guid.NewGuid();
            var tenantId = tenant?.Id ?? Guid.NewGuid();
            var tenantSlug = tenant?.Slug ?? "default-tenant";
            var userEmail = user?.Email ?? "admin@ocap.io";
            var userName = user?.FullName ?? "Administrador OCAP";

            var roles = await _identityService.GetUserRolesAsync(userId, tenantId, cancellationToken);
            var permissions = await _identityService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Subject, userId.ToString());
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Email, userEmail);
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Name, userName);
            AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
            AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
            AddClaimWithDestinations(identity, "user_id", userId.ToString());
            AddClaimWithDestinations(identity, "auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            foreach (var role in roles)
            {
                AddClaimWithDestinations(identity, "roles", role.Name);
                AddClaimWithDestinations(identity, ClaimTypes.Role, role.Name);
            }

            foreach (var perm in permissions)
            {
                AddClaimWithDestinations(identity, "permissions", perm);
            }

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            await _auditService.LogSecurityEventAsync(tenantId, userId, "OAuth.ClientCredentialsTokenIssued", $"Token de cliente emitido para {clientId}", "ConnectController", true, cancellationToken);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var userIdString = result.Principal?.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            var tenantIdString = result.Principal?.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "El token de refresco no es válido o ha expirado."
                });
            }

            var tenantId = Guid.TryParse(tenantIdString, out var parsedTenantId) ? parsedTenantId : Guid.NewGuid();
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
            var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            var tenantSlug = tenant?.Slug ?? "default-tenant";
            var userEmail = user?.Email ?? "admin@ocap.io";
            var userName = user?.FullName ?? "Administrador OCAP";

            var roles = await _identityService.GetUserRolesAsync(userId, tenantId, cancellationToken);
            var permissions = await _identityService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Subject, userId.ToString());
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Email, userEmail);
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Name, userName);
            AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
            AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
            AddClaimWithDestinations(identity, "user_id", userId.ToString());
            AddClaimWithDestinations(identity, "auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            foreach (var role in roles)
            {
                AddClaimWithDestinations(identity, "roles", role.Name);
                AddClaimWithDestinations(identity, ClaimTypes.Role, role.Name);
            }

            foreach (var perm in permissions)
            {
                AddClaimWithDestinations(identity, "permissions", perm);
            }

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            await _auditService.LogSecurityEventAsync(tenantId, userId, "OAuth.RefreshTokenIssued", "Token de acceso refrescado exitosamente", "ConnectController", true, cancellationToken);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                await _auditService.LogSecurityEventAsync(
                    Guid.Empty, Guid.Empty, "Authorization.Denied", "Canje de código de autorización fallido o expirado", "ConnectController", false, cancellationToken);

                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "El código de autorización no es válido, ha expirado o ya fue utilizado."
                });
            }

            var userIdString = result.Principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            var tenantIdString = result.Principal.FindFirst("tenant_id")?.Value;
            var nonce = result.Principal.FindFirst(OpenIddictConstants.Claims.Nonce)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "El sujeto del token no es válido."
                });
            }

            var tenantId = Guid.TryParse(tenantIdString, out var parsedTenantId) ? parsedTenantId : Guid.NewGuid();
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
            var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            var tenantSlug = tenant?.Slug ?? "default-tenant";
            var userEmail = user?.Email ?? "admin@ocap.io";
            var userName = user?.FullName ?? "Administrador OCAP";

            var roles = await _identityService.GetUserRolesAsync(userId, tenantId, cancellationToken);
            var permissions = await _identityService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Subject, userId.ToString());
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Email, userEmail);
            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Name, userName);
            AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
            AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
            AddClaimWithDestinations(identity, "user_id", userId.ToString());
            AddClaimWithDestinations(identity, "auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            if (!string.IsNullOrEmpty(nonce))
            {
                AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Nonce, nonce);
            }

            foreach (var role in roles)
            {
                AddClaimWithDestinations(identity, "roles", role.Name);
                AddClaimWithDestinations(identity, ClaimTypes.Role, role.Name);
            }

            foreach (var perm in permissions)
            {
                AddClaimWithDestinations(identity, "permissions", perm);
            }

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            await _auditService.LogSecurityEventAsync(
                tenantId, userId, "Authorization.Consumed", $"Código de autorización canjeado exitosamente por tokens para cliente '{request.ClientId}'", "ConnectController", true, cancellationToken);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "El tipo de concesión solicitado no está soportado."
        });
    }

    private static void AddClaimWithDestinations(ClaimsIdentity identity, string type, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var claim = new Claim(type, value);
        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
        identity.AddClaim(claim);
    }
}
