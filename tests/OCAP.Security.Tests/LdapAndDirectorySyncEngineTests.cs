using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class LdapAndDirectorySyncEngineTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task SaveLdapConfig_And_TestConnection_ShouldSucceed()
    {
        var dbContext = GetInMemoryDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var service = new LdapService(dbContext, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var dto = new SaveLdapConfigDto("ldap.corp.local", 636, true, "cn=admin,dc=corp,dc=local", "secret", "dc=corp,dc=local");
        var config = await service.SaveLdapConfigAsync(tenantId, dto);

        Assert.NotNull(config);
        Assert.Equal("ldap.corp.local", config.Server);

        var testResult = await service.TestConnectionAsync(tenantId, dto);
        Assert.True(testResult);
    }

    [Fact]
    public async Task TriggerSyncJob_ExecutesFullSyncAndRecordsHistory()
    {
        var dbContext = GetInMemoryDbContext();
        var auditMock = new Mock<ISecurityAuditService>();
        var engine = new DirectorySyncEngine(dbContext, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var status = await engine.TriggerSyncJobAsync(tenantId, "LDAP", "Full");

        Assert.NotNull(status);
        Assert.Equal("Completed", status.Status);
        Assert.Equal(10, status.TotalUsersSynced);

        var history = await engine.GetSyncHistoryAsync(tenantId);
        Assert.NotEmpty(history);
        Assert.Equal("Completed", history[0].Status);
    }
}
