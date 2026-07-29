using Microsoft.AspNetCore.Mvc;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class ScimControllerTests
{
    [Fact]
    public async Task GetUsers_ReturnsOkResultWithScimListResponse()
    {
        var tenantId = Guid.NewGuid();
        var scimMock = new Mock<IScimService>();
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var expectedResponse = new ScimListResponseDto<ScimUserDto>(
            totalResults: 1,
            startIndex: 1,
            itemsPerPage: 1,
            schemas: new List<string> { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            Resources: new List<ScimUserDto>
            {
                new ScimUserDto("1", "ext-1", "user@test.com", null, new List<ScimEmailDto>(), true, new List<string>())
            }
        );

        scimMock.Setup(s => s.GetUsersAsync(tenantId, 1, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new ScimController(scimMock.Object, tenantContextMock.Object);

        var result = await controller.GetUsers(1, 100, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResponse, okResult.Value);
    }
}
