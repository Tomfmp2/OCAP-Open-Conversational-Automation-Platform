using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class WebAuthnServiceTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task CompleteRegistration_And_GetDevices_ShouldManagePasskeys()
    {
        var dbContext = GetInMemoryDbContext();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new WebAuthnService(dbContext, auditMock.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new WebAuthnRegisterRequestDto("YubiKey 5C", "cred_12345", "PEM_PUBLIC_KEY");
        var device = await service.CompleteRegistrationAsync(tenantId, userId, request);

        Assert.NotNull(device);
        Assert.Equal("YubiKey 5C", device.DeviceName);
        Assert.Equal("cred_12345", device.CredentialId);

        var devices = await service.GetRegisteredDevicesAsync(tenantId, userId);
        Assert.Single(devices);

        var deleteResult = await service.DeleteDeviceAsync(tenantId, userId, device.Id);
        Assert.True(deleteResult);

        var finalDevices = await service.GetRegisteredDevicesAsync(tenantId, userId);
        Assert.Empty(finalDevices);
    }
}
