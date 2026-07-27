using FluentAssertions;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyHashAndSalt()
    {
        // Act
        var (hash, salt) = _hasher.HashPassword("SuperSecret123!");

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword2026!";
        var (hash, salt) = _hasher.HashPassword(password);

        // Act
        var isValid = _hasher.VerifyPassword(password, hash, salt);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var (hash, salt) = _hasher.HashPassword("CorrectPassword");

        // Act
        var isValid = _hasher.VerifyPassword("WrongPassword", hash, salt);

        // Assert
        isValid.Should().BeFalse();
    }
}
