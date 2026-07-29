using Microsoft.AspNetCore.Mvc;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Tests.Endpoints;

public class SamlControllerTests
{
    [Fact]
    public async Task GetSpMetadata_ReturnsContentResultWithXml()
    {
        var tenantId = Guid.NewGuid();
        var samlMock = new Mock<ISamlService>();
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var expectedXml = "<md:EntityDescriptor xmlns:md=\"urn:oasis:names:tc:SAML:2.0:metadata\"></md:EntityDescriptor>";
        samlMock.Setup(s => s.GetSpMetadataXmlAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedXml);

        var controller = new SamlController(samlMock.Object, tenantContextMock.Object);

        var result = await controller.GetSpMetadata(tenantId, CancellationToken.None);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.StartsWith("application/xml", contentResult.ContentType);
        Assert.Equal(expectedXml, contentResult.Content);
    }
}
