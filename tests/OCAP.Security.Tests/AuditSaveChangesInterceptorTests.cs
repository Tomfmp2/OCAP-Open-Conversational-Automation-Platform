using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Interceptors;

namespace OCAP.Security.Tests;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task AuditSaveChangesInterceptor_ShouldResolveTenantAndUser_AndCreateAuditLog()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(claimsPrincipal);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        var interceptor = new AuditSaveChangesInterceptor(accessorMock.Object);

        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var dbContext = new OCAPDbContext(options);

        // Act
        var user = new User(Guid.NewGuid(), "audit_user");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Assert
        var auditLog = await dbContext.AuditLogs.IgnoreQueryFilters().FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.TenantId.Should().Be(tenantId);
        auditLog.UserId.Should().Be(userId);
        auditLog.Action.Should().Contain("Entity_Added_User");
    }
}
