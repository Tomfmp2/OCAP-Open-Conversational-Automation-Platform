using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class UserManagementServiceTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task InviteUserAsync_ShouldCreateUser_AndLogAuditEvent()
    {
        var dbContext = GetInMemoryDbContext();
        var passwordHasher = new PasswordHasher();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new UserManagementService(dbContext, passwordHasher, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var request = new InviteUserRequestDto("invited@ocap.io", "Invited User", "Operator");
        var result = await service.InviteUserAsync(tenantId, request);

        Assert.NotNull(result);
        Assert.Equal("invited@ocap.io", result.Email);
        Assert.True(result.IsActive);
        Assert.False(result.IsLocked);

        auditMock.Verify(a => a.LogSecurityEventAsync(tenantId, result.Id, "User.Invited", It.IsAny<string>(), "UserManagementService", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockAndUnlockUser_ShouldUpdateUserLockStatus()
    {
        var dbContext = GetInMemoryDbContext();
        var passwordHasher = new PasswordHasher();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new UserManagementService(dbContext, passwordHasher, auditMock.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (hash, salt) = passwordHasher.HashPassword("Pass123!");
        var user = new UserIdentity(userId, tenantId, "user@ocap.io", hash, salt, "Test User");
        dbContext.UserIdentities.Add(user);
        await dbContext.SaveChangesAsync();

        var lockResult = await service.LockUserAsync(tenantId, userId);
        Assert.True(lockResult);

        var lockedUser = await service.GetUserByIdAsync(tenantId, userId);
        Assert.NotNull(lockedUser);
        Assert.True(lockedUser!.IsLocked);

        var unlockResult = await service.UnlockUserAsync(tenantId, userId);
        Assert.True(unlockResult);

        var unlockedUser = await service.GetUserByIdAsync(tenantId, userId);
        Assert.NotNull(unlockedUser);
        Assert.False(unlockedUser!.IsLocked);
    }

    [Fact]
    public async Task ActivateAndDeactivateUser_ShouldUpdateActiveState()
    {
        var dbContext = GetInMemoryDbContext();
        var passwordHasher = new PasswordHasher();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new UserManagementService(dbContext, passwordHasher, auditMock.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (hash, salt) = passwordHasher.HashPassword("Pass123!");
        var user = new UserIdentity(userId, tenantId, "user@ocap.io", hash, salt, "Test User");
        dbContext.UserIdentities.Add(user);
        await dbContext.SaveChangesAsync();

        var deactivateResult = await service.DeactivateUserAsync(tenantId, userId);
        Assert.True(deactivateResult);

        var inactiveUser = await service.GetUserByIdAsync(tenantId, userId);
        Assert.False(inactiveUser!.IsActive);

        var activateResult = await service.ActivateUserAsync(tenantId, userId);
        Assert.True(activateResult);

        var activeUser = await service.GetUserByIdAsync(tenantId, userId);
        Assert.True(activeUser!.IsActive);
    }
}
