using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class ExternalIdentityResolverTests
{
    private OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenIdentityExists_ReturnsUserId()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var externalIdentity = new ExternalIdentity(
            Guid.NewGuid(),
            tenantId,
            userId,
            "Telegram",
            "839292929");

        dbContext.ExternalIdentities.Add(externalIdentity);
        await dbContext.SaveChangesAsync();

        var resolver = new ExternalIdentityResolver(dbContext);

        // Act
        var resolvedUserId = await resolver.ResolveUserIdAsync(tenantId, "Telegram", "839292929");

        // Assert
        resolvedUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ResolveUserIdAsync_WhenIdentityDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var resolver = new ExternalIdentityResolver(dbContext);

        // Act
        var resolvedUserId = await resolver.ResolveUserIdAsync(tenantId, "Telegram", "non_existing_id");

        // Assert
        resolvedUserId.Should().BeNull();
    }

    [Fact]
    public async Task LinkExternalIdentityAsync_WhenIdentityDoesNotExist_CreatesNewAndReturnsTrue()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resolver = new ExternalIdentityResolver(dbContext);

        // Act
        var result = await resolver.LinkExternalIdentityAsync(tenantId, userId, "Telegram", "839292929");

        // Assert
        result.Should().BeTrue();
        var stored = await dbContext.ExternalIdentities.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ExternalId == "839292929");
        stored.Should().NotBeNull();
        stored!.UserId.Should().Be(userId);
        stored.Provider.Should().Be("Telegram");
    }

    [Fact]
    public async Task LinkExternalIdentityAsync_MultiTenantIsolation_ResolvesCorrectUserPerTenant()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var resolver = new ExternalIdentityResolver(dbContext);

        await resolver.LinkExternalIdentityAsync(tenantA, userA, "Telegram", "839292929");
        await resolver.LinkExternalIdentityAsync(tenantB, userB, "Telegram", "839292929");

        // Act & Assert — Identical ExternalId in different tenants resolves to different users
        var resolvedA = await resolver.ResolveUserIdAsync(tenantA, "Telegram", "839292929");
        var resolvedB = await resolver.ResolveUserIdAsync(tenantB, "Telegram", "839292929");

        resolvedA.Should().Be(userA);
        resolvedB.Should().Be(userB);
        resolvedA.Value.Should().NotBe(resolvedB!.Value);
    }
}
