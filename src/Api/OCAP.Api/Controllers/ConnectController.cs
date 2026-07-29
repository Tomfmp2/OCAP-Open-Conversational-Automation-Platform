using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace OCAP.Api.Controllers;

// Controlador para Servidor de Autorización OAuth2 / OpenID Connect (OpenIddict)
[ApiController]
public class ConnectController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly OCAPDbContext _dbContext;

    public ConnectController(
        IOpenIddictApplicationManager applicationManager,
        IIdentityService identityService,
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService,
        OCAPDbContext dbContext)
    {
        _applicationManager = applicationManager ?? throw new ArgumentNullException(nameof(applicationManager));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

            var roles = await _identityService.GetUserRolesAsync(userId, tenantId, cancellationToken);
            var permissions = await _identityService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Subject, userId.ToString());
            AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
            AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
            AddClaimWithDestinations(identity, "user_id", userId.ToString());

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
            var tenantSlug = tenant?.Slug ?? "default-tenant";

            var roles = await _identityService.GetUserRolesAsync(userId, tenantId, cancellationToken);
            var permissions = await _identityService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            AddClaimWithDestinations(identity, OpenIddictConstants.Claims.Subject, userId.ToString());
            AddClaimWithDestinations(identity, "tenant_id", tenantId.ToString());
            AddClaimWithDestinations(identity, "tenant_slug", tenantSlug);
            AddClaimWithDestinations(identity, "user_id", userId.ToString());

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
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "El flujo Authorization Code (y PKCE) se implementará en una entrega posterior."
            });
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "El tipo de concesión solicitado no está soportado."
        });
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public IActionResult AuthorizePlaceholder()
    {
        return StatusCode(501, new
        {
            error = "not_implemented",
            error_description = "Authorization Code Flow y PKCE no están implementados en esta fase de CAP-10."
        });
    }

    private static void AddClaimWithDestinations(ClaimsIdentity identity, string type, string value)
    {
        var claim = new Claim(type, value);
        claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
        identity.AddClaim(claim);
    }
}
