using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class ApiKeyServiceTests
{
    private static OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task CreateApiKey_ReturnsRawSecretAndValidHashEntity()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new ApiKeyService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var (rawKey, entity) = service.CreateApiKey(tenantId, userId, "WhatsApp Integration", TimeSpan.FromDays(30));

        // Assert
        rawKey.Should().StartWith("ocap_live_");
        entity.Should().NotBeNull();
        entity.TenantId.Should().Be(tenantId);
        entity.IsActive.Should().BeTrue();

        var validated = await service.ValidateApiKeyAsync(rawKey);
        validated.Should().NotBeNull();
        validated!.Id.Should().Be(entity.Id);
    }

    [Fact]
    public async Task ValidateApiKey_EnforcesScopes()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new ApiKeyService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var scopes = new[] { "workflows:read", "workflows:execute" };

        // Act
        var (rawKey, entity) = service.CreateApiKey(tenantId, userId, "Scoped Key", scopes, TimeSpan.FromDays(1));

        // Assert
        entity.HasScope("workflows:read").Should().BeTrue();
        entity.HasScope("workflows:execute").Should().BeTrue();
        entity.HasScope("admin:all").Should().BeFalse();

        var validWithScope = await service.ValidateApiKeyAsync(rawKey, "workflows:read");
        validWithScope.Should().NotBeNull();

        var invalidWithScope = await service.ValidateApiKeyAsync(rawKey, "admin:all");
        invalidWithScope.Should().BeNull();
    }

    [Fact]
    public async Task RevokeApiKey_DeactivatesKey()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new ApiKeyService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (rawKey, entity) = service.CreateApiKey(tenantId, userId, "Key To Revoke", TimeSpan.FromDays(1));

        // Act
        var revoked = await service.RevokeApiKeyAsync(entity.Id, tenantId);

        // Assert
        revoked.Should().BeTrue();
        entity.IsRevoked.Should().BeTrue();
        entity.IsActive.Should().BeFalse();

        var validatedAfterRevoke = await service.ValidateApiKeyAsync(rawKey);
        validatedAfterRevoke.Should().BeNull();
    }
}
