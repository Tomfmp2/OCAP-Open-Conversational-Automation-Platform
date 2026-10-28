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
        using var db = CreateDbContext();
        var service = new RefreshTokenService(db);
        var (tenantId, userId) = await SeedUserAsync(db);

        var token1 = await service.CreateRefreshTokenAsync(userId, TimeSpan.FromMinutes(30));
        token1.UserId.Should().Be(userId);

        var token2 = await service.ValidateAndRotateRefreshTokenAsync(token1.Token);
        token2.Should().NotBeNull();
        token2!.UserId.Should().Be(userId);
        token2.Token.Should().NotBe(token1.Token);

        var stored = await db.RefreshTokens.AsNoTracking().ToListAsync();
        stored.Should().HaveCount(2).And.OnlyContain(t => t.TenantId == tenantId);
        // La base solo tiene hashes: ninguno de los valores entregados aparece tal cual.
        stored.Should().NotContain(t => t.TokenHash == token1.Token || t.TokenHash == token2.Token);
        var old = stored.Single(t => t.TokenHash == RefreshToken.Hash(token1.Token));
        old.IsRevoked.Should().BeTrue();
        old.ReplacedByTokenHash.Should().Be(RefreshToken.Hash(token2.Token));
        stored.Select(t => t.FamilyId).Distinct().Should().ContainSingle();

        (await service.RevokeUserRefreshTokensAsync(userId)).Should().Be(1);
        (await service.ValidateAndRotateRefreshTokenAsync(token2.Token)).Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenService_ReusingARotatedToken_RevokesTheWholeSession()
    {
        using var db = CreateDbContext();
        var service = new RefreshTokenService(db);
        var (_, userId) = await SeedUserAsync(db);

        var stolen = await service.CreateRefreshTokenAsync(userId);
        var legitimate = await service.ValidateAndRotateRefreshTokenAsync(stolen.Token);

        // Quien copió el token original lo presenta después de la rotación: no obtiene sesión y la del usuario se cierra.
        (await service.ValidateAndRotateRefreshTokenAsync(stolen.Token)).Should().BeNull();
        (await service.ValidateAndRotateRefreshTokenAsync(legitimate!.Token)).Should().BeNull();
        (await db.RefreshTokens.AsNoTracking().AnyAsync(t => !t.IsRevoked)).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenService_OtherSessionsOfTheUser_SurviveAReuse()
    {
        using var db = CreateDbContext();
        var service = new RefreshTokenService(db);
        var (_, userId) = await SeedUserAsync(db);

        var laptop = await service.CreateRefreshTokenAsync(userId);
        var phone = await service.CreateRefreshTokenAsync(userId);
        await service.ValidateAndRotateRefreshTokenAsync(laptop.Token);
        await service.ValidateAndRotateRefreshTokenAsync(laptop.Token);

        (await service.ValidateAndRotateRefreshTokenAsync(phone.Token)).Should().NotBeNull();
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedUserAsync(OCAPDbContext db)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new Tenant(tenantId, "Test", "test"));
        db.UserIdentities.Add(new UserIdentity(userId, tenantId, "rt@test.com", "hash", "salt", "RT User"));
        await db.SaveChangesAsync();
        return (tenantId, userId);
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
