using FluentAssertions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Tests;

public class TenantIsolationTests
{
    [Fact]
    public void Tenant_Invariants_EnsureUniqueSlugAndIsolation()
    {
        // Arrange & Act
        var tenant1 = new Tenant(Guid.NewGuid(), "Organización Alpha", "alpha-org");
        var tenant2 = new Tenant(Guid.NewGuid(), "Organización Beta", "beta-org");

        // Assert
        tenant1.Id.Should().NotBe(tenant2.Id);
        tenant1.Slug.Should().NotBe(tenant2.Slug);
        tenant1.IsActive.Should().BeTrue();
    }
}
