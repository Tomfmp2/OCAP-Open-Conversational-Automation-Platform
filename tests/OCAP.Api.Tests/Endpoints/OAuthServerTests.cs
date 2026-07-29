using System.Net;
using System.Security.Claims;
using FluentAssertions;
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
    public async Task Exchange_AuthorizationCode_ReturnsUnsupportedGrantType()
    {
        // Arrange
        using var db = CreateDbContext();
        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _auditMock.Object,
            db);

        var request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.AuthorizationCode
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
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as OpenIddictResponse;
        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenIddictConstants.Errors.UnsupportedGrantType);
    }

    [Fact]
    public void AuthorizePlaceholder_Returns501NotImplemented()
    {
        // Arrange
        using var db = CreateDbContext();
        var controller = new ConnectController(
            _appManagerMock.Object,
            _identityMock.Object,
            _refreshTokenMock.Object,
            _auditMock.Object,
            db);

        // Act
        var result = controller.AuthorizePlaceholder();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotImplemented);
    }
}
