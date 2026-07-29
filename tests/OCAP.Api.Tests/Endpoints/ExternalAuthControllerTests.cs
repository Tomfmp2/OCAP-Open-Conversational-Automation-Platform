using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class ExternalAuthControllerTests
{
    private readonly Mock<IExternalAuthenticationService> _externalAuthMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();

    [Fact]
    public async Task GetEnabledProviders_ReturnsOkWithProvidersList()
    {
        // Arrange
        var expected = new List<ExternalProviderInfoDto>
        {
            new("google", "Google", true, null),
            new("microsoft", "Microsoft", true, null)
        };

        _externalAuthMock.Setup(x => x.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ExternalAuthController(_externalAuthMock.Object, _tenantContextMock.Object);

        // Act
        var result = await controller.GetEnabledProviders(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Challenge_ValidProvider_ReturnsOkWithChallengeDto()
    {
        // Arrange
        var expected = new ExternalAuthChallengeDto("google", "https://accounts.google.com/o/oauth2/v2/auth?...", "state123");
        _externalAuthMock.Setup(x => x.InitiateChallengeAsync("google", It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ExternalAuthController(_externalAuthMock.Object, _tenantContextMock.Object);

        // Act
        var result = await controller.Challenge("google", "https://app.com/callback", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expected);
    }
}
