using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class OAuthServerTests
{
    private readonly Mock<IOpenIddictApplicationManager> _appManagerMock = new();
    private readonly Mock<IIdentityService> _identityMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenMock = new();
    private readonly Mock<IConsentService> _consentMock = new();
    private readonly Mock<ISecurityAuditService> _auditMock = new();

    private static OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task Exchange_ClientCredentials_IssuesTokenForClientSubject()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(tenantId, "Acme Corp", "acme");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var appMock = new object();
        _appManagerMock.Setup(x => x.FindByClientIdAsync("test_client_id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(appMock);

        var props = new Dictionary<string, JsonElement>
        {
            ["tenant_id"] = JsonSerializer.SerializeToElement(tenantId.ToString())
        }.ToImmutableDictionary();
        _appManagerMock.Setup(x => x.GetPropertiesAsync(appMock, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<ImmutableDictionary<string, JsonElement>>(props));

        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _consentMock.Object,
            _auditMock.Object,
            db);

        var request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
            ClientId = "test_client_id"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Exchange(CancellationToken.None);

        result.Should().BeOfType<SignInResult>();
        var signInResult = (SignInResult)result;
        var principal = signInResult.Principal;
        principal.Should().NotBeNull();
        principal!.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.Should().Be("test_client_id");
        principal.FindFirst("client_id")?.Value.Should().Be("test_client_id");
        principal.FindFirst("tenant_id")?.Value.Should().Be(tenantId.ToString());
        principal.FindFirst("tenant_slug")?.Value.Should().Be("acme");
        principal.FindFirst("user_id").Should().BeNull();
    }

    [Fact]
    public async Task Exchange_AuthorizationCode_WithoutAuthentication_ReturnsInvalidGrant()
    {
        using var db = CreateDbContext();
        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _consentMock.Object,
            _auditMock.Object,
            db);

        var request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.AuthorizationCode,
            ClientId = "test_client"
        };

        var serviceProviderMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Fail("No authentication"));
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Exchange(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as OpenIddictResponse;
        response!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
    }

    [Fact]
    public async Task Authorize_MissingPkce_ReturnsBadRequest()
    {
        using var db = CreateDbContext();
        var clientId = "client_pkce_test";
        var appMock = new object();
        _appManagerMock.Setup(x => x.FindByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appMock);

        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _consentMock.Object,
            _auditMock.Object,
            db);

        var request = new OpenIddictRequest
        {
            ResponseType = OpenIddictConstants.ResponseTypes.Code,
            ClientId = clientId,
            RedirectUri = "https://client.app/callback"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Authorize(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as OpenIddictResponse;
        response!.Error.Should().Be(OpenIddictConstants.Errors.InvalidRequest);
    }

    [Fact]
    public async Task Authorize_ValidPkceRequest_WithAuthenticatedUser_GrantsConsentAndSignIn()
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant(tenantId, "Acme Corp", "acme");
        var user = new UserIdentity(userId, tenantId, "user@acme.com", "hash", "salt", "Test User");
        db.Tenants.Add(tenant);
        db.UserIdentities.Add(user);
        await db.SaveChangesAsync();

        var clientId = "client_pkce_test";
        var appMock = new object();
        _appManagerMock.Setup(x => x.FindByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appMock);
        _appManagerMock.Setup(x => x.ValidateRedirectUriAsync(appMock, "https://client.app/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _identityMock.Setup(x => x.GetUserRolesAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { new Role(Guid.NewGuid(), tenantId, "User", "Desc", new[] { "Workflows.Read" }) });
        _identityMock.Setup(x => x.GetUserPermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Workflows.Read" });

        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _consentMock.Object,
            _auditMock.Object,
            db);

        var request = new OpenIddictRequest
        {
            ResponseType = OpenIddictConstants.ResponseTypes.Code,
            ClientId = clientId,
            RedirectUri = "https://client.app/callback",
            CodeChallenge = "E9Mel-vBsRCgI653255p26_g_4567890123456789012",
            CodeChallengeMethod = OpenIddictConstants.CodeChallengeMethods.Sha256,
            Scope = "openid profile email"
        };

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString())
        }, authenticationType: JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Authorize(CancellationToken.None);

        result.Should().BeOfType<SignInResult>();
        var principal = ((SignInResult)result).Principal;
        principal!.FindFirst("tenant_id")?.Value.Should().Be(tenantId.ToString());
        principal.FindFirst("user_id")?.Value.Should().Be(userId.ToString());
        principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.Should().Be(userId.ToString());

        _consentMock.Verify(x => x.GrantConsentAsync(tenantId, userId, clientId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Authorize_WithoutAuthenticatedUser_ReturnsChallenge()
    {
        using var db = CreateDbContext();
        var clientId = "client_pkce_test";
        var appMock = new object();
        _appManagerMock.Setup(x => x.FindByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appMock);
        _appManagerMock.Setup(x => x.ValidateRedirectUriAsync(appMock, "https://client.app/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var serviceProviderMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), JwtBearerDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Fail("anon"));
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _consentMock.Object,
            _auditMock.Object,
            db);

        var request = new OpenIddictRequest
        {
            ResponseType = OpenIddictConstants.ResponseTypes.Code,
            ClientId = clientId,
            RedirectUri = "https://client.app/callback",
            CodeChallenge = "E9Mel-vBsRCgI653255p26_g_4567890123456789012",
            CodeChallengeMethod = OpenIddictConstants.CodeChallengeMethods.Sha256
        };

        var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Authorize(CancellationToken.None);
        result.Should().BeOfType<ChallengeResult>();
    }
}
