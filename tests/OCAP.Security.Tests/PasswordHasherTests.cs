using System.Security.Cryptography;
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

    [Fact]
    public void NewHashes_RecordTheSchemeAndIterations_AndVerify()
    {
        var (hash, salt) = _hasher.HashPassword("una-contraseña-larga-1");

        hash.Should().StartWith($"pbkdf2-sha256${PasswordHasher.Iterations}$");
        _hasher.VerifyPassword("una-contraseña-larga-1", hash, salt).Should().BeTrue();
        _hasher.VerifyPassword("otra", hash, salt).Should().BeFalse();
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Fact]
    public void LegacyHashes_StillVerify_AndAskForARehash()
    {
        // Formato anterior: solo el base64 de PBKDF2-SHA256 con 100.000 iteraciones.
        var salt = RandomNumberGenerator.GetBytes(16);
        var legacy = Convert.ToBase64String(
            Rfc2898DeriveBytes.Pbkdf2("vieja-contraseña-1", salt, 100_000, HashAlgorithmName.SHA256, 32));

        _hasher.VerifyPassword("vieja-contraseña-1", legacy, Convert.ToBase64String(salt)).Should().BeTrue();
        _hasher.NeedsRehash(legacy).Should().BeTrue();
    }

    [Fact]
    public void AMalformedHash_DoesNotVerify()
    {
        _hasher.VerifyPassword("x-contraseña-1", "pbkdf2-sha256$abc$!!", Convert.ToBase64String(new byte[16])).Should().BeFalse();
    }
}
