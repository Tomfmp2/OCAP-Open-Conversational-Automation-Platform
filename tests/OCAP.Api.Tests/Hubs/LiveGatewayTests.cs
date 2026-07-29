using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using OCAP.Api.Hubs;
using OCAP.Api.Services;
using OCAP.Core.Events;
using OCAP.Infrastructure.Events;
using Xunit;

namespace OCAP.Api.Tests.Hubs;

public class LiveGatewayTests
{
    private readonly Mock<IHubContext<EventsHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _hubClientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();

    public LiveGatewayTests()
    {
        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
    }

    [Fact]
    public async Task LiveGateway_BroadcastsWorkflowAndAgentEventsToTenantGroupOnly()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var subscriber = new LiveGatewayEventSubscriber(eventBus, _hubContextMock.Object);
        await subscriber.StartAsync(CancellationToken.None);

        var tenantId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var workflowStarted = new WorkflowStartedEvent(executionId, workflowId, tenantId, userId, null);

        // Act
        await eventBus.PublishAsync(workflowStarted);

        // Assert
        var expectedGroup = EventsHub.GetTenantGroupName(tenantId);
        _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.AtLeastOnce());
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            nameof(WorkflowStartedEvent),
            It.Is<object[]>(args => args.Length > 0 && Equals(args[0], workflowStarted)),
            default), Times.Once());
    }

    [Fact]
    public async Task LiveGateway_BroadcastsAll8PlatformEventsWithTenantIsolation()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var subscriber = new LiveGatewayEventSubscriber(eventBus, _hubContextMock.Object);
        await subscriber.StartAsync(CancellationToken.None);

        var tenantId = Guid.NewGuid();
        var groupName = EventsHub.GetTenantGroupName(tenantId);

        var events = new IEvent[]
        {
            new WorkflowStartedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, Guid.NewGuid(), null),
            new WorkflowCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, "{}", 120),
            new WorkflowFailedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, "Simulated Error"),
            new NodeExecutedEvent(Guid.NewGuid(), "step-1", "HTTP Request", "HttpRequestNode", true, 45, "{}", null, tenantId),
            new AgentStartedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, Guid.NewGuid(), "Hola"),
            new AgentCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, Guid.NewGuid(), "Respuesta", true, 300),
            new MessageReceivedEvent("Telegram", "user-123", "Hola bot", tenantId),
            new MessageSentEvent("Telegram", "user-123", "Hola usuario", true, tenantId)
        };

        // Act
        foreach (var @event in events)
        {
            await eventBus.PublishAsync((dynamic)@event);
        }

        // Assert
        _hubClientsMock.Verify(c => c.Group(groupName), Times.Exactly(events.Length * 2)); // 1 for EventName, 1 for "ReceiveEvent"
    }

    [Fact]
    public void EventsHub_FormatsTenantGroupNameCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var groupName = EventsHub.GetTenantGroupName(tenantId);

        // Assert
        groupName.Should().Be($"tenant_{tenantId}");
    }
}
