using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class SamlServiceTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task GetSpMetadataXmlAsync_ReturnsValidSamlMetadataXml()
    {
        var dbContext = GetInMemoryDbContext();
        var jwtMock = new Mock<IJwtTokenService>();
        var refreshMock = new Mock<IRefreshTokenService>();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new SamlService(dbContext, jwtMock.Object, refreshMock.Object, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var xml = await service.GetSpMetadataXmlAsync(tenantId);

        Assert.NotNull(xml);
        Assert.Contains("md:EntityDescriptor", xml);
        Assert.Contains($"https://ocap.io/saml/sp/{tenantId}", xml);
        Assert.Contains("AssertionConsumerService", xml);
    }

    [Fact]
    public async Task SaveSamlProviderConfigAsync_And_InitiateSpLogin_ShouldWorkCorrectly()
    {
        var dbContext = GetInMemoryDbContext();
        var jwtMock = new Mock<IJwtTokenService>();
        var refreshMock = new Mock<IRefreshTokenService>();
        var auditMock = new Mock<ISecurityAuditService>();

        var service = new SamlService(dbContext, jwtMock.Object, refreshMock.Object, auditMock.Object);
        var tenantId = Guid.NewGuid();

        var saveDto = new SaveSamlProviderConfigDto(
            EntityId: "https://idp.okta.com/exk12345",
            SsoServiceUrl: "https://idp.okta.com/app/sso/saml"
        );

        var config = await service.SaveSamlProviderConfigAsync(tenantId, saveDto);
        Assert.NotNull(config);
        Assert.Equal("https://idp.okta.com/exk12345", config.EntityId);

        var redirect = await service.InitiateSpLoginAsync(tenantId);
        Assert.NotNull(redirect);
        Assert.Equal("https://idp.okta.com/app/sso/saml", redirect.SsoUrl);
        Assert.NotNull(redirect.SamlRequestBase64);
    }
}
