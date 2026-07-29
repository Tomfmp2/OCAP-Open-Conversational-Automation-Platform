using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class IdentityFoundationTests
{
    private readonly Mock<ISecurityAuditService> _auditMock = new();

    private static OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task RefreshTokenService_CreatesRotatesAndRevokesTokens()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new RefreshTokenService(db);
        var userId = Guid.NewGuid();

        // Act - Create
        var token1 = await service.CreateRefreshTokenAsync(userId, TimeSpan.FromMinutes(30));

        // Assert
        token1.Should().NotBeNull();
        token1.UserId.Should().Be(userId);
        token1.IsActive.Should().BeTrue();

        // Act - Rotate
        var token2 = await service.ValidateAndRotateRefreshTokenAsync(token1.Token);

        // Assert
        token2.Should().NotBeNull();
        token2!.UserId.Should().Be(userId);
        token2.Token.Should().NotBe(token1.Token);

        var oldTokenInDb = await db.RefreshTokens.FirstAsync(t => t.Id == token1.Id);
        oldTokenInDb.IsRevoked.Should().BeTrue();
        oldTokenInDb.ReplacedByToken.Should().Be(token2.Token);

        // Act - Revoke User All
        var revokedCount = await service.RevokeUserRefreshTokensAsync(userId);
        revokedCount.Should().Be(1);

        var token2InDb = await db.RefreshTokens.FirstAsync(t => t.Id == token2.Id);
        token2InDb.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task IdentityService_ManagesRolesClaimsAndPermissionsWithTenantIsolation()
    {
        // Arrange
        using var db = CreateDbContext();
        var identityService = new IdentityService(db, _auditMock.Object);

        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var adminRole = new Role(Guid.NewGuid(), tenant1, "Admin", "Administrador de Tenant", new[] { "Workflows.Write", "Agents.Manage" });
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();

        // Act - Assign Role
        var assigned = await identityService.AssignRoleToUserAsync(userId, adminRole.Id, tenant1);
        assigned.Should().BeTrue();

        // Assert - Roles & Permissions
        var rolesTenant1 = await identityService.GetUserRolesAsync(userId, tenant1);
        rolesTenant1.Should().HaveCount(1);
        rolesTenant1[0].Name.Should().Be("Admin");

        var permsTenant1 = await identityService.GetUserPermissionsAsync(userId, tenant1);
        permsTenant1.Should().Contain("Workflows.Write");

        var hasPerm = await identityService.HasPermissionAsync(userId, tenant1, "Workflows.Write");
        hasPerm.Should().BeTrue();

        // Assert - Tenant Isolation (Tenant 2 has no roles/perms for this user)
        var rolesTenant2 = await identityService.GetUserRolesAsync(userId, tenant2);
        rolesTenant2.Should().BeEmpty();

        var hasPermTenant2 = await identityService.HasPermissionAsync(userId, tenant2, "Workflows.Write");
        hasPermTenant2.Should().BeFalse();

        // Act - Add Claims
        var claim = await identityService.AddOrUpdateUserClaimAsync(userId, tenant1, "department", "Engineering");
        claim.Should().NotBeNull();
        claim.ClaimValue.Should().Be("Engineering");

        var claims = await identityService.GetUserClaimsAsync(userId, tenant1);
        claims.Should().HaveCount(1);
        claims[0].ClaimType.Should().Be("department");

        // Act - Remove Role
        var removed = await identityService.RemoveRoleFromUserAsync(userId, adminRole.Id, tenant1);
        removed.Should().BeTrue();

        var rolesAfterRemove = await identityService.GetUserRolesAsync(userId, tenant1);
        rolesAfterRemove.Should().BeEmpty();
    }
}
