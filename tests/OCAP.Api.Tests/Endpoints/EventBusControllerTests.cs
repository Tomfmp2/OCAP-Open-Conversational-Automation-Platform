using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class EventBusControllerTests
{
    private OCAPDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task GetStatus_ReturnsHealthyStatus()
    {
        var transportMock = new Mock<IEventTransport>();
        transportMock.Setup(t => t.HealthCheckAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        transportMock.Setup(t => t.ProviderName).Returns("InMemory");

        var deadLetterMock = new Mock<IMessageDeadLetterHandler>();
        var dbContext = GetInMemoryDbContext();

        var controller = new EventBusController(transportMock.Object, deadLetterMock.Object, dbContext);

        var result = await controller.GetStatus(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
