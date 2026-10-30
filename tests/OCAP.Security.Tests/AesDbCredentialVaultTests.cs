using System.Security.Cryptography;
using System.Text;
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
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
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
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
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

    private const string MasterKey = "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!";

    [Fact]
    public async Task RetrieveSecret_WhenTheCipherTextWasAltered_ReturnsNull()
    {
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, MasterKey);
        var tenantId = Guid.NewGuid();
        var secretRef = await vault.StoreSecretAsync(tenantId, "ApiKey", "valor-original");
        secretRef.Should().StartWith("vault:aesgcm:");

        var parts = secretRef.Split(':');
        var payload = Convert.FromBase64String(parts[^1]);
        payload[^1] ^= 0x01;
        parts[^1] = Convert.ToBase64String(payload);

        (await vault.RetrieveSecretAsync(tenantId, string.Join(':', parts))).Should().BeNull();
    }

    [Fact]
    public async Task RetrieveSecret_WhenTheSecretNameWasSwapped_ReturnsNull()
    {
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, MasterKey);
        var tenantId = Guid.NewGuid();
        var secretRef = await vault.StoreSecretAsync(tenantId, "webhook-hmac-secret", "valor");

        (await vault.RetrieveSecretAsync(tenantId, secretRef.Replace("webhook-hmac-secret", "ldap-bind-password"))).Should().BeNull();
    }

    [Fact]
    public async Task RetrieveSecret_StillReadsReferencesWrittenWithAesCbc()
    {
        // Formato anterior: vault:aes256 con clave SHA-256(maestra_tenant) e IV | texto cifrado.
        var tenantId = Guid.NewGuid();
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes($"{MasterKey}_{tenantId:N}"));
        aes.GenerateIV();
        var plain = Encoding.UTF8.GetBytes("token-antiguo");
        var cipher = aes.CreateEncryptor().TransformFinalBlock(plain, 0, plain.Length);
        var legacyRef = $"vault:aes256:{tenantId:N}:BotToken:{Convert.ToBase64String(aes.IV.Concat(cipher).ToArray())}";

        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, MasterKey);

        (await vault.RetrieveSecretAsync(tenantId, legacyRef)).Should().Be("token-antiguo");
    }
}
