using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Tests;

public class WorkflowEngineTests
{
    private readonly Mock<IWorkflowDefinitionRepository> _definitionRepoMock;
    private readonly Mock<IWorkflowExecutionRepository> _executionRepoMock;
    private readonly WorkflowEngine _engine;
    private readonly Guid _definitionId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public WorkflowEngineTests()
    {
        var nodes = new IWorkflowNodeExecutor[]
        {
            new StartNode(),
            new EndNode(),
            new ConditionNode(),
            new DelayNode()
        };
        var nodeResolver = new WorkflowNodeExecutorResolver(nodes);

        _definitionRepoMock = new Mock<IWorkflowDefinitionRepository>();
        _executionRepoMock = new Mock<IWorkflowExecutionRepository>();

        var definition = new WorkflowDefinition(_definitionId, _tenantId, "Test Workflow");
        definition.AddStep(new WorkflowStep(Guid.NewGuid(), "step1", "Start", WorkflowNodeType.Start, "{}"));
        definition.AddStep(new WorkflowStep(Guid.NewGuid(), "step2", "End", WorkflowNodeType.End, "{}"));
        definition.AddTransition(new WorkflowTransition(Guid.NewGuid(), "step1", "step2", "true"));

        _definitionRepoMock
            .Setup(r => r.GetByIdAsync(_definitionId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _engine = new WorkflowEngine(
            nodeResolver,
            NullLogger<WorkflowEngine>.Instance,
            _definitionRepoMock.Object,
            _executionRepoMock.Object
        );
    }

    [Fact]
    public async Task StartWorkflowAsync_ExecutesWorkflowAndCompletes()
    {
        // Arrange
        var context = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };

        // Act
        var execution = await _engine.StartWorkflowAsync(_definitionId, context);

        // Assert
        execution.Should().NotBeNull();
        execution.Status.Should().Be(WorkflowStatus.Completed);
        execution.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task PauseAndResumeWorkflow_ChangesStateCorrectly()
    {
        // Arrange
        var context = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        var execution = new WorkflowExecution(Guid.NewGuid(), _definitionId, _tenantId, context.UserId, null, "step1");
        
        _executionRepoMock
            .Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
            
        _executionRepoMock
            .Setup(r => r.GetVariablesAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowVariable>());

        // Act & Assert
        var paused = await _engine.PauseWorkflowAsync(execution.Id);
        paused.Status.Should().Be(WorkflowStatus.Paused);

        var resumed = await _engine.ResumeWorkflowAsync(execution.Id, context);
        resumed.Status.Should().Be(WorkflowStatus.Completed); // because it will resume and hit the end node
    }

    [Fact]
    public async Task CancelWorkflow_UpdatesStatusToCancelled()
    {
        // Arrange
        var context = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        var execution = new WorkflowExecution(Guid.NewGuid(), _definitionId, _tenantId, context.UserId, null, "step1");
        
        _executionRepoMock
            .Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        // Act
        var cancelled = await _engine.CancelWorkflowAsync(execution.Id);

        // Assert
        cancelled.Status.Should().Be(WorkflowStatus.Cancelled);
    }
}
