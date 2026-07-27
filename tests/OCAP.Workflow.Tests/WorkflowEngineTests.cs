using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Tests;

public class WorkflowEngineTests
{
    private readonly WorkflowEngine _engine;

    public WorkflowEngineTests()
    {
        var nodes = new IWorkflowNode[]
        {
            new StartNode(),
            new EndNode(),
            new ConditionNode(),
            new DelayNode()
        };

        _engine = new WorkflowEngine(nodes, NullLogger<WorkflowEngine>.Instance);
    }

    [Fact]
    public async Task StartWorkflowAsync_ExecutesWorkflowAndCompletes()
    {
        // Arrange
        var context = new WorkflowContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        // Act
        var execution = await _engine.StartWorkflowAsync(Guid.NewGuid(), context);

        // Assert
        execution.Should().NotBeNull();
        execution.Status.Should().Be(WorkflowStatus.Completed);
        execution.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task PauseAndResumeWorkflow_ChangesStateCorrectly()
    {
        // Arrange
        var context = new WorkflowContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var execution = await _engine.StartWorkflowAsync(Guid.NewGuid(), context);

        // Act & Assert
        var paused = await _engine.PauseWorkflowAsync(execution.Id);
        paused.Status.Should().Be(WorkflowStatus.Paused);

        var resumed = await _engine.ResumeWorkflowAsync(execution.Id, context);
        resumed.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public async Task CancelWorkflow_UpdatesStatusToCancelled()
    {
        // Arrange
        var context = new WorkflowContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var execution = await _engine.StartWorkflowAsync(Guid.NewGuid(), context);

        // Act
        var cancelled = await _engine.CancelWorkflowAsync(execution.Id);

        // Assert
        cancelled.Status.Should().Be(WorkflowStatus.Cancelled);
    }
}
