using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class UserContextTests
{
    [Fact]
    public void HttpUserContext_ShouldExtractUserIdAndNameFromClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@ocap.ai")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(claimsPrincipal);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        var userContext = new HttpUserContext(accessorMock.Object);

        // Act & Assert
        userContext.UserId.Should().Be(userId);
        userContext.UserName.Should().Be("testuser");
        userContext.Email.Should().Be("test@ocap.ai");
        userContext.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void HttpUserContext_WhenNoHttpContext_ShouldReturnDefaults()
    {
        // Arrange
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var userContext = new HttpUserContext(accessorMock.Object);

        // Act & Assert
        userContext.UserId.Should().Be(Guid.Empty);
        userContext.UserName.Should().Be("System");
        userContext.Email.Should().BeEmpty();
        userContext.IsAuthenticated.Should().BeFalse();
    }
}
