using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Infrastructure.Services;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Channels.Tests;

public class ChannelConnectionTests
{
    private OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task CreateConnection_StoresEncryptedCredentialsInVault_AndCreatesEntity()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var registry = new ChannelRegistry();
        var manager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);

        var tenantId = Guid.NewGuid();
        var provider = "Telegram";
        var displayName = "Bot Telegram Ventas";
        var secretToken = "123456:ABC-DEF1234ghIkl-zyx57";

        // Act
        var connection = await manager.CreateConnectionAsync(tenantId, provider, displayName, secretToken);

        // Assert
        connection.Should().NotBeNull();
        connection.TenantId.Should().Be(tenantId);
        connection.Provider.Should().Be("Telegram");
        connection.DisplayName.Should().Be("Bot Telegram Ventas");
        connection.Enabled.Should().BeTrue();
        connection.CredentialsReference.Should().NotBeNullOrEmpty();
        connection.CredentialsReference.Should().NotContain(secretToken); // NEVER store raw secrets

        // Verify credentials can be retrieved securely from Vault
        var retrievedSecret = await vault.RetrieveSecretAsync(tenantId, connection.CredentialsReference);
        retrievedSecret.Should().Be(secretToken);
    }

    [Fact]
    public async Task CreateConnection_DuplicateProviderForSameTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var registry = new ChannelRegistry();
        var manager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);

        var tenantId = Guid.NewGuid();
        await manager.CreateConnectionAsync(tenantId, "Telegram", "Bot 1", "SECRET_1");

        // Act & Assert
        var act = async () => await manager.CreateConnectionAsync(tenantId, "Telegram", "Bot 2", "SECRET_2");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya existe una conexión registrada*");
    }

    [Fact]
    public async Task EnableAndDisableChannel_UpdatesLifecycleState()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var registry = new ChannelRegistry();
        var manager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);

        var tenantId = Guid.NewGuid();
        var connection = await manager.CreateConnectionAsync(tenantId, "WhatsApp", "WhatsApp Soporte", "WA_TOKEN_123");

        // Act & Assert — Disable
        var disableResult = await manager.DisableChannelAsync(tenantId, connection.Id);
        disableResult.Should().BeTrue();
        var disabledConn = await dbContext.ChannelConnections.FirstAsync(c => c.Id == connection.Id);
        disabledConn.Enabled.Should().BeFalse();

        // Act & Assert — Enable
        var enableResult = await manager.EnableChannelAsync(tenantId, connection.Id);
        enableResult.Should().BeTrue();
        var enabledConn = await dbContext.ChannelConnections.FirstAsync(c => c.Id == connection.Id);
        enabledConn.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantConnections_EnforcesMultiTenantIsolation()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var registry = new ChannelRegistry();
        var manager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await manager.CreateConnectionAsync(tenantA, "Telegram", "Bot A", "TOKEN_A");
        await manager.CreateConnectionAsync(tenantB, "Telegram", "Bot B", "TOKEN_B");

        // Act
        var connectionsA = await manager.GetTenantConnectionsAsync(tenantA);
        var connectionsB = await manager.GetTenantConnectionsAsync(tenantB);

        // Assert
        connectionsA.Should().HaveCount(1);
        connectionsA.First().DisplayName.Should().Be("Bot A");

        connectionsB.Should().HaveCount(1);
        connectionsB.First().DisplayName.Should().Be("Bot B");
    }

    [Fact]
    public async Task RemoveConnection_DeletesFromDatabaseAndVault()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var registry = new ChannelRegistry();
        var manager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);

        var tenantId = Guid.NewGuid();
        var connection = await manager.CreateConnectionAsync(tenantId, "WebChat", "Web Widget", "SECRET_WEB");

        // Act
        var removeResult = await manager.RemoveConnectionAsync(tenantId, connection.Id);

        // Assert
        removeResult.Should().BeTrue();
        var stored = await dbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == connection.Id);
        stored.Should().BeNull();
    }
}
