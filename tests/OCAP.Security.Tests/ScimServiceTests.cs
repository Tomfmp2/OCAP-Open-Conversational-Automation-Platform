using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class ScimServiceTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesNewScimUser()
    {
        var dbContext = GetInMemoryDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var service = new ScimService(dbContext, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var dto = new ScimUserDto(
            id: string.Empty,
            externalId: "ext-123",
            userName: "scimuser@example.com",
            name: new ScimNameDto("SCIM User", "User", "SCIM"),
            emails: new List<ScimEmailDto> { new ScimEmailDto("scimuser@example.com", "work", true) },
            active: true,
            schemas: new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" }
        );

        var created = await service.CreateUserAsync(tenantId, dto);

        Assert.NotNull(created);
        Assert.Equal("scimuser@example.com", created.userName);

        var fetched = await service.GetUserByIdAsync(tenantId, created.id);
        Assert.NotNull(fetched);
        Assert.Equal("scimuser@example.com", fetched.userName);
    }

    [Fact]
    public async Task CreateGroupAsync_CreatesNewScimGroup()
    {
        var dbContext = GetInMemoryDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var service = new ScimService(dbContext, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var dto = new ScimGroupDto(
            id: string.Empty,
            externalId: "ext-group-1",
            displayName: "DevOps Team",
            members: new List<ScimGroupMemberDto>(),
            schemas: new List<string> { "urn:ietf:params:scim:schemas:core:2.0:Group" }
        );

        var created = await service.CreateGroupAsync(tenantId, dto);

        Assert.NotNull(created);
        Assert.Equal("DevOps Team", created.displayName);

        var list = await service.GetGroupsAsync(tenantId);
        Assert.Equal(1, list.totalResults);
    }
}
