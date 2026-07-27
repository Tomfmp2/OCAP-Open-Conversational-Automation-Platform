using FluentAssertions;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class ApiKeyServiceTests
{
    private readonly ApiKeyService _service = new();

    [Fact]
    public async Task CreateApiKey_ReturnsRawSecretAndValidHashEntity()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var (rawKey, entity) = _service.CreateApiKey(tenantId, userId, "WhatsApp Integration", TimeSpan.FromDays(30));

        // Assert
        rawKey.Should().StartWith("ocap_live_");
        entity.Should().NotBeNull();
        entity.TenantId.Should().Be(tenantId);
        entity.IsActive.Should().BeTrue();

        var validated = await _service.ValidateApiKeyAsync(rawKey);
        validated.Should().NotBeNull();
        validated!.Id.Should().Be(entity.Id);
    }
}
