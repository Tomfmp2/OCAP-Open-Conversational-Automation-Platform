using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class ExternalAuthenticationServiceTests
{
    private readonly Mock<IExternalAuthProvider> _providerMock = new();
    private readonly Mock<IExternalIdentityResolver> _resolverMock = new();
    private readonly Mock<IIdentityService> _identityMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
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
    public async Task GetEnabledProviders_ReturnsOnlyActiveProviders()
    {
        // Arrange
        using var db = CreateDbContext();
        _providerMock.Setup(p => p.ProviderName).Returns("google");
        _providerMock.Setup(p => p.DisplayName).Returns("Google Workspace");
        _providerMock.Setup(p => p.IsEnabled).Returns(true);

        var disabledMock = new Mock<IExternalAuthProvider>();
        disabledMock.Setup(p => p.ProviderName).Returns("disabled_provider");
        disabledMock.Setup(p => p.IsEnabled).Returns(false);

        var service = new ExternalAuthenticationService(
            new[] { _providerMock.Object, disabledMock.Object },
            _resolverMock.Object,
            _identityMock.Object,
            _jwtMock.Object,
            _refreshTokenMock.Object,
            _auditMock.Object,
            db,
            Options.Create(new ExternalAuthenticationSettings())
        );

        // Act
        var providers = await service.GetEnabledProvidersAsync();

        // Assert
        providers.Should().HaveCount(1);
        providers[0].Name.Should().Be("google");
        providers[0].DisplayName.Should().Be("Google Workspace");
    }

    [Fact]
    public async Task ProcessCallback_ExistingUser_GeneratesTokensAndAudits()
    {
        // Arrange
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenant = new Tenant(tenantId, "Acme Corp", "acme");
        var user = new UserIdentity(userId, tenantId, "user@acme.com", "hash", "salt", "Test User");
        db.Tenants.Add(tenant);
        db.UserIdentities.Add(user);
        await db.SaveChangesAsync();

        _providerMock.Setup(p => p.ProviderName).Returns("google");
        _providerMock.Setup(p => p.IsEnabled).Returns(true);

        var userPayload = new ExternalUserPayloadDto("google", "ext_12345", "user@acme.com", "Test User", null, null);
        _providerMock.Setup(p => p.ProcessCallbackAsync("code_123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPayload);

        _resolverMock.Setup(r => r.ResolveUserIdAsync(tenantId, "google", "ext_12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _identityMock.Setup(i => i.GetUserRolesAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());
        _identityMock.Setup(i => i.GetUserPermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<UserIdentity>(), It.IsAny<Tenant>(), It.IsAny<Role>(), It.IsAny<IEnumerable<string>>()))
            .Returns("access_token_jwt");

        _refreshTokenMock.Setup(r => r.CreateRefreshTokenAsync(userId, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken(Guid.NewGuid(), userId, "refresh_token_123", DateTime.UtcNow.AddDays(7)));

        var service = new ExternalAuthenticationService(
            new[] { _providerMock.Object },
            _resolverMock.Object,
            _identityMock.Object,
            _jwtMock.Object,
            _refreshTokenMock.Object,
            _auditMock.Object,
            db,
            Options.Create(new ExternalAuthenticationSettings())
        );

        var request = new ExternalAuthCallbackRequestDto("google", "code_123", "state_123");

        // Act
        var result = await service.ProcessCallbackAsync(request, tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("access_token_jwt");
        result.RefreshToken.Should().Be("refresh_token_123");
        result.UserId.Should().Be(userId);

        _auditMock.Verify(a => a.LogSecurityEventAsync(tenantId, userId, "ExternalAuth.Success", It.IsAny<string>(), "ExternalAuthenticationService", true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
