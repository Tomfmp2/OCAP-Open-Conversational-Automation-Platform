using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class ConsentServiceTests
{
    private static OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task GrantConsent_SavesConsentToDatabaseAndAudits()
    {
        // Arrange
        using var db = CreateDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var service = new ConsentService(db, auditMock.Object);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var clientId = "test_client";
        var scopes = new[] { "openid", "profile" };

        // Act
        var consent = await service.GrantConsentAsync(tenantId, userId, clientId, scopes);

        // Assert
        consent.Should().NotBeNull();
        consent.TenantId.Should().Be(tenantId);
        consent.UserId.Should().Be(userId);
        consent.ClientId.Should().Be(clientId);
        consent.GrantedScopes.Should().Be("openid profile");
        consent.IsRevoked.Should().BeFalse();

        var savedConsent = await db.UserConsents.FirstOrDefaultAsync(c => c.Id == consent.Id);
        savedConsent.Should().NotBeNull();

        auditMock.Verify(x => x.LogSecurityEventAsync(
            tenantId, userId, "Authorization.ConsentGranted", It.IsAny<string>(), "ConsentService", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasConsent_WithMatchingScopes_ReturnsTrue()
    {
        // Arrange
        using var db = CreateDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var service = new ConsentService(db, auditMock.Object);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var clientId = "test_client";
        var scopes = new[] { "openid", "profile", "email" };

        await service.GrantConsentAsync(tenantId, userId, clientId, scopes);

        // Act
        var hasConsent = await service.HasConsentAsync(tenantId, userId, clientId, new[] { "openid", "email" });

        // Assert
        hasConsent.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeConsent_SetsIsRevokedTrueAndAudits()
    {
        // Arrange
        using var db = CreateDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var service = new ConsentService(db, auditMock.Object);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var clientId = "test_client";

        var consent = await service.GrantConsentAsync(tenantId, userId, clientId, new[] { "openid" });

        // Act
        var revoked = await service.RevokeConsentAsync(consent.Id, tenantId);

        // Assert
        revoked.Should().BeTrue();
        var dbConsent = await db.UserConsents.FirstOrDefaultAsync(c => c.Id == consent.Id);
        dbConsent!.IsRevoked.Should().BeTrue();

        auditMock.Verify(x => x.LogSecurityEventAsync(
            tenantId, userId, "Authorization.ConsentRevoked", It.IsAny<string>(), "ConsentService", true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
