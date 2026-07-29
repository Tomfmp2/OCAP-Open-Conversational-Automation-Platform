using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class MfaAndWebAuthnControllerTests
{
    [Fact]
    public async Task WebAuthnController_GetDevices_ReturnsRegisteredDevices()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var webAuthnMock = new Mock<IWebAuthnService>();
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var expectedDevices = new List<WebAuthnDeviceDto>
        {
            new(Guid.NewGuid(), "cred_1", "MacBook Touch ID", DateTime.UtcNow, null)
        };

        webAuthnMock.Setup(w => w.GetRegisteredDevicesAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDevices);

        var controller = new WebAuthnController(webAuthnMock.Object, tenantContextMock.Object);
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Email, "user@ocap.io")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };

        var result = await controller.GetDevices(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var devices = Assert.IsAssignableFrom<IReadOnlyList<WebAuthnDeviceDto>>(okResult.Value);
        Assert.Single(devices);
    }
}
