using Microsoft.AspNetCore.Mvc;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class AdminControllersTests
{
    [Fact]
    public async Task UsersController_GetUsers_ReturnsOkResultWithUsers()
    {
        var tenantId = Guid.NewGuid();
        var userMock = new Mock<IUserManagementService>();
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var expectedUsers = new List<UserDetailDto>
        {
            new(Guid.NewGuid(), tenantId, "admin@ocap.io", "Admin User", true, false, true, DateTime.UtcNow)
        };

        userMock.Setup(u => u.GetUsersAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        var controller = new UsersController(userMock.Object, tenantContextMock.Object);

        var result = await controller.GetUsers(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<UserDetailDto>>(okResult.Value);
        Assert.Single(users);
    }

    [Fact]
    public async Task GroupsController_CreateGroup_ReturnsCreatedResult()
    {
        var tenantId = Guid.NewGuid();
        var groupMock = new Mock<IGroupService>();
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var request = new CreateGroupRequestDto("Devs", "Developers Group");
        var expectedGroup = new GroupDto(Guid.NewGuid(), tenantId, "Devs", "Developers Group", DateTime.UtcNow, 0);

        groupMock.Setup(g => g.CreateGroupAsync(tenantId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        var controller = new GroupsController(groupMock.Object, tenantContextMock.Object);

        var result = await controller.CreateGroup(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var group = Assert.IsType<GroupDto>(createdResult.Value);
        Assert.Equal("Devs", group.Name);
    }
}
