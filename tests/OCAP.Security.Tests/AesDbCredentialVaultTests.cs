using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class AesDbCredentialVaultTests
{
    [Fact]
    public async Task StoreAndRetrieveSecret_WhenValidTenant_ReturnsOriginalPlainTextSecret()
    {
        // Arrange
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance);
        var tenantId = Guid.NewGuid();
        var secretKey = "BotToken";
        var originalSecret = "123456789:ABCdefGHIjklMNOpqrsTUVwxyz_SECRET";

        // Act
        var secretRef = await vault.StoreSecretAsync(tenantId, secretKey, originalSecret);
        var retrievedSecret = await vault.RetrieveSecretAsync(tenantId, secretRef);

        // Assert
        secretRef.Should().NotBeNullOrEmpty();
        secretRef.Should().NotContain(originalSecret); // Plaintext NEVER exposed in reference
        retrievedSecret.Should().Be(originalSecret);
    }

    [Fact]
    public async Task RetrieveSecret_WithDifferentTenantId_EnforcesMultiTenantIsolation_ReturnsNull()
    {
        // Arrange
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var secretKey = "ApiKey";
        var originalSecret = "SUPER_SECRET_KEY_FOR_TENANT_A";

        // Act
        var secretRef = await vault.StoreSecretAsync(tenantA, secretKey, originalSecret);
        var retrievedByTenantB = await vault.RetrieveSecretAsync(tenantB, secretRef);

        // Assert
        retrievedByTenantB.Should().BeNull();
    }
}
