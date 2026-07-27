using FluentAssertions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Tests;

public class WorkflowNodeTests
{
    [Fact]
    public async Task StartNode_ReturnsSuccessStepResult()
    {
        // Arrange
        var node = new StartNode();
        var step = new WorkflowStep(Guid.NewGuid(), "start_1", "Inicio", OCAP.Workflow.Domain.Enums.WorkflowNodeType.Start);

        // Act
        var result = await node.ExecuteAsync(step, new WorkflowContext());

        // Assert
        result.Success.Should().BeTrue();
        result.NextStepId.Should().Be("next");
    }

    [Fact]
    public async Task EndNode_ReturnsCompletedResult()
    {
        // Arrange
        var node = new EndNode();
        var step = new WorkflowStep(Guid.NewGuid(), "end_1", "Fin", OCAP.Workflow.Domain.Enums.WorkflowNodeType.End);

        // Act
        var result = await node.ExecuteAsync(step, new WorkflowContext());

        // Assert
        result.Success.Should().BeTrue();
        result.NextStepId.Should().Be("end");
    }
}
