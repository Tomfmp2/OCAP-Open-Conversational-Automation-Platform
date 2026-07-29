using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
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
    public async Task Exchange_ClientCredentials_IssuesTokenWithMultiTenantClaims()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant(tenantId, "Acme Corp", "acme");
        var user = new UserIdentity(userId, tenantId, "user@acme.com", "hash", "salt", "Test User");
        db.Tenants.Add(tenant);
        db.UserIdentities.Add(user);
        await db.SaveChangesAsync();

        _identityMock.Setup(x => x.GetUserRolesAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { new Role(Guid.NewGuid(), tenantId, "Admin", "Desc", new[] { "Workflows.Read" }) });

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
            GrantType = OpenIddictConstants.GrantTypes.ClientCredentials,
            ClientId = "test_client_id"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = request
            }
        });

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.Exchange(CancellationToken.None);

        // Assert
        result.Should().BeOfType<SignInResult>();
        var signInResult = (SignInResult)result;
        signInResult.AuthenticationScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var principal = signInResult.Principal;
        principal.Should().NotBeNull();
        principal!.FindFirst("tenant_id")?.Value.Should().Be(tenantId.ToString());
        principal.FindFirst("tenant_slug")?.Value.Should().Be("acme");
        principal.FindFirst("user_id")?.Value.Should().Be(userId.ToString());
        principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task Exchange_AuthorizationCode_WithoutAuthentication_ReturnsInvalidGrant()
    {
        // Arrange
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

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProviderMock.Object
        };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = request
            }
        });

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.Exchange(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as OpenIddictResponse;
        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
    }

    [Fact]
    public async Task Authorize_MissingPkce_ReturnsBadRequest()
    {
        // Arrange
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
            Transaction = new OpenIddictServerTransaction
            {
                Request = request
            }
        });

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.Authorize(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as OpenIddictResponse;
        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenIddictConstants.Errors.InvalidRequest);
    }

    [Fact]
    public async Task Authorize_ValidPkceRequest_GrantsConsentAndSignIn()
    {
        // Arrange
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

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = request
            }
        });

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.Authorize(CancellationToken.None);

        // Assert
        result.Should().BeOfType<SignInResult>();
        var signInResult = (SignInResult)result;
        signInResult.AuthenticationScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var principal = signInResult.Principal;
        principal.Should().NotBeNull();
        principal!.FindFirst("tenant_id")?.Value.Should().Be(tenantId.ToString());
        principal.FindFirst("tenant_slug")?.Value.Should().Be("acme");
        principal.FindFirst("user_id")?.Value.Should().Be(userId.ToString());
        principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value.Should().Be(userId.ToString());

        _consentMock.Verify(x => x.GrantConsentAsync(tenantId, userId, clientId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
