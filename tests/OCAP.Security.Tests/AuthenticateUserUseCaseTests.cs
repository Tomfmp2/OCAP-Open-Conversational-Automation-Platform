using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Security.Abstractions;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class AuthenticateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ReturnsAuthenticationResult()
    {
        var hasher = new PasswordHasher();
        var (hash, salt) = hasher.HashPassword("Pass123!");
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new UserIdentity(userId, tenantId, "admin@ocap.io", hash, salt, "Admin");
        var tenant = new Tenant(tenantId, "Org", "org");
        var role = new Role(roleId, tenantId, "Admin", "Admin", new[] { "Dashboard.Read" });

        var query = new Mock<IUserAuthenticationQuery>();
        query.Setup(q => q.FindByEmailAsync("admin@ocap.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAuthenticationRecord(user, tenant, role));

        var refresh = new Mock<IRefreshTokenService>();
        refresh.Setup(r => r.CreateRefreshTokenAsync(userId, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken(Guid.NewGuid(), userId, "refresh-token", DateTime.UtcNow.AddDays(7)));

        var jwtService = new JwtTokenService("SUPER_SECRET_KEY_FOR_SECURITY_TESTS_2026_OCAP_JWT");
        var auditService = new SecurityAuditService(NullLogger<SecurityAuditService>.Instance);
        var useCase = new AuthenticateUserUseCase(hasher, jwtService, auditService, query.Object, refresh.Object);

        var result = await useCase.ExecuteAsync("admin@ocap.io", "Pass123!", "192.168.1.100");

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be("admin@ocap.io");
        result.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownUser_ReturnsNull()
    {
        var hasher = new PasswordHasher();
        var query = new Mock<IUserAuthenticationQuery>();
        query.Setup(q => q.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAuthenticationRecord?)null);
        var refresh = new Mock<IRefreshTokenService>();
        var jwtService = new JwtTokenService("SUPER_SECRET_KEY_FOR_SECURITY_TESTS_2026_OCAP_JWT");
        var auditService = new SecurityAuditService(NullLogger<SecurityAuditService>.Instance);
        var useCase = new AuthenticateUserUseCase(hasher, jwtService, auditService, query.Object, refresh.Object);

        var result = await useCase.ExecuteAsync("nobody@ocap.io", "Pass123!", "127.0.0.1");
        result.Should().BeNull();
    }
}
