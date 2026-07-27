using FluentAssertions;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new("SUPER_SECRET_KEY_FOR_SECURITY_TESTS_2026_OCAP_JWT");

    [Fact]
    public void GenerateAccessToken_ReturnsValidSignedJwt()
    {
        // Arrange
        var tenant = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");
        var user = new UserIdentity(Guid.NewGuid(), tenant.Id, "user@test.io", "hash", "salt", "Test User");
        var role = new Role(Guid.NewGuid(), tenant.Id, "Admin", "Test Role", new[] { "Conversation.Read" });

        // Act
        var token = _service.GenerateAccessToken(user, tenant, role, role.Permissions);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var principal = _service.ValidateToken(token);
        principal.Should().NotBeNull();
        principal!.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value.Should().Be("user@test.io");
        principal.FindFirst("tenant_slug")?.Value.Should().Be("test-tenant");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsActiveRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var refreshToken = _service.GenerateRefreshToken(userId);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.IsActive.Should().BeTrue();
        refreshToken.UserId.Should().Be(userId);
    }
}
