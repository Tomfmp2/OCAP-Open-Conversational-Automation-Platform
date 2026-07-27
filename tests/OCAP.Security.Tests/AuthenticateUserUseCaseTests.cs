using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class AuthenticateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ReturnsAuthenticationResult()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var jwtService = new JwtTokenService("SUPER_SECRET_KEY_FOR_SECURITY_TESTS_2026_OCAP_JWT");
        var auditService = new SecurityAuditService(NullLogger<SecurityAuditService>.Instance);
        var useCase = new AuthenticateUserUseCase(hasher, jwtService, auditService);

        // Act
        var result = await useCase.ExecuteAsync("admin@ocap.io", "Pass123!", "192.168.1.100");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("admin@ocap.io");
        result.RoleName.Should().Be("Admin");
    }
}
