using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class MfaServiceTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task SetupMfaAsync_ShouldGenerateSecretAndQrUri()
    {
        var dbContext = GetInMemoryDbContext();
        var totpService = new TotpService();
        var vaultMock = new Mock<ICredentialVault>();
        vaultMock.Setup(v => v.StoreSecretAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid t, string k, string val, CancellationToken c) => "ref_" + k);
        vaultMock.Setup(v => v.RetrieveSecretAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid t, string r, CancellationToken c) => "SECRET_KEY_MOCK");

        var passwordHasher = new PasswordHasher();
        var auditMock = new Mock<ISecurityAuditService>();

        var mfaService = new MfaService(dbContext, totpService, vaultMock.Object, passwordHasher, auditMock.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var setup = await mfaService.SetupMfaAsync(tenantId, userId, "user@ocap.io");

        Assert.NotNull(setup);
        Assert.NotNull(setup.Secret);
        Assert.StartsWith("otpauth://totp/", setup.QrCodeUri);

        var dbSettings = await dbContext.UserMfaSettings.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId);
        Assert.NotNull(dbSettings);
        Assert.False(dbSettings!.IsEnabled);
    }
}
