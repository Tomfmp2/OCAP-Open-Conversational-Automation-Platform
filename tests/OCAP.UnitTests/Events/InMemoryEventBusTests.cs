using OCAP.Core.Events;
using OCAP.Infrastructure.Events;
using Xunit;

namespace OCAP.UnitTests.Events;

public class InMemoryEventBusTests
{
    [Fact]
    public async Task PublishAsync_InvokesRegisteredDelegateHandler()
    {
        // Arrange
        var bus = new InMemoryEventBus();
        var received = false;
        WorkflowStartedEvent? capturedEvent = null;

        bus.Subscribe<WorkflowStartedEvent>((ev, ct) =>
        {
            received = true;
            capturedEvent = ev;
            return Task.CompletedTask;
        });

        var execId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var evt = new WorkflowStartedEvent(execId, defId, tenantId, userId, null);

        // Act
        await bus.PublishAsync(evt);

        // Assert
        Assert.True(received);
        Assert.NotNull(capturedEvent);
        Assert.Equal(execId, capturedEvent.ExecutionId);
        Assert.Equal(defId, capturedEvent.WorkflowDefinitionId);
        Assert.Equal(tenantId, capturedEvent.TenantId);
        Assert.Equal(userId, capturedEvent.UserId);
    }

    [Fact]
    public async Task PublishAsync_InvokesInterfaceHandler()
    {
        // Arrange
        var bus = new InMemoryEventBus();
        var mockHandler = new TestWorkflowCompletedHandler();
        bus.Subscribe<WorkflowCompletedEvent>(mockHandler);

        var evt = new WorkflowCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{\"res\":1}", 150.5);

        // Act
        await bus.PublishAsync(evt);

        // Assert
        Assert.Single(mockHandler.HandledEvents);
        Assert.Equal(evt.ExecutionId, mockHandler.HandledEvents[0].ExecutionId);
    }

    [Fact]
    public async Task PublishAsync_ContinuesExecutionIfOneHandlerFails()
    {
        // Arrange
        var bus = new InMemoryEventBus();
        var secondHandlerInvoked = false;

        bus.Subscribe<WorkflowFailedEvent>((ev, ct) => throw new InvalidOperationException("Handler 1 error"));
        bus.Subscribe<WorkflowFailedEvent>((ev, ct) =>
        {
            secondHandlerInvoked = true;
            return Task.CompletedTask;
        });

        var evt = new WorkflowFailedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sample failure");

        // Act
        await bus.PublishAsync(evt);

        // Assert
        Assert.True(secondHandlerInvoked);
    }

    [Fact]
    public async Task PublishAsync_AllDomainEventsCanBePublishedAndSubscribed()
    {
        // Arrange
        var bus = new InMemoryEventBus();
        var count = 0;

        bus.Subscribe<WorkflowStartedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<WorkflowCompletedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<WorkflowFailedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<NodeExecutedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<AgentStartedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<AgentCompletedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<MessageReceivedEvent>((e, c) => { count++; return Task.CompletedTask; });
        bus.Subscribe<MessageSentEvent>((e, c) => { count++; return Task.CompletedTask; });

        var tenantId = Guid.NewGuid();

        // Act
        await bus.PublishAsync(new WorkflowStartedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, Guid.NewGuid(), null));
        await bus.PublishAsync(new WorkflowCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, "{}", 10.0));
        await bus.PublishAsync(new WorkflowFailedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, "err"));
        await bus.PublishAsync(new NodeExecutedEvent(Guid.NewGuid(), "step-1", "Step 1", "HttpRequest", true, 5.0, "{}", null, tenantId));
        await bus.PublishAsync(new AgentStartedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, Guid.NewGuid(), "hello"));
        await bus.PublishAsync(new AgentCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, Guid.NewGuid(), "world", true, 12.0));
        await bus.PublishAsync(new MessageReceivedEvent("Telegram", "123", "hi", tenantId));
        await bus.PublishAsync(new MessageSentEvent("Telegram", "123", "hi back", true, tenantId));

        // Assert
        Assert.Equal(8, count);
    }

    private class TestWorkflowCompletedHandler : IEventHandler<WorkflowCompletedEvent>
    {
        public List<WorkflowCompletedEvent> HandledEvents { get; } = new();

        public Task HandleAsync(WorkflowCompletedEvent @event, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(@event);
            return Task.CompletedTask;
        }
    }
}
