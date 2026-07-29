using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class GroupServiceTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task CreateGroup_And_AddUserToGroup_ShouldWorkCorrectly()
    {
        var dbContext = GetInMemoryDbContext();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new GroupService(dbContext, auditMock.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var user = new UserIdentity(userId, tenantId, "member@ocap.io", "hash", "salt", "Member User");
        dbContext.UserIdentities.Add(user);
        await dbContext.SaveChangesAsync();

        var groupDto = await service.CreateGroupAsync(tenantId, new CreateGroupRequestDto("Engineering", "Dev team"));
        Assert.NotNull(groupDto);
        Assert.Equal("Engineering", groupDto.Name);

        var addResult = await service.AddUserToGroupAsync(tenantId, groupDto.Id, userId);
        Assert.True(addResult);

        var updatedGroup = await service.GetGroupByIdAsync(tenantId, groupDto.Id);
        Assert.NotNull(updatedGroup);
        Assert.Equal(1, updatedGroup!.UserCount);

        var removeResult = await service.RemoveUserFromGroupAsync(tenantId, groupDto.Id, userId);
        Assert.True(removeResult);

        var finalGroup = await service.GetGroupByIdAsync(tenantId, groupDto.Id);
        Assert.Equal(0, finalGroup!.UserCount);
    }
}
