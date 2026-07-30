using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Tools.Abstractions;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Expressions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Tests;

public class AgentAndToolIntegrationTests
{
    [Fact]
    public async Task ToolNode_InvokesRegisteredTools()
    {
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ToolResult.Ok("ok"));

        var mockRegistry = new Mock<IToolRegistry>();
        mockRegistry.Setup(r => r.GetTool(It.IsAny<string>())).Returns(mockTool.Object);

        var toolNode = new ToolNode(mockRegistry.Object, new WorkflowExpressionEvaluator());
        var step = new WorkflowStep(Guid.NewGuid(), "tool_1", "Herramienta", WorkflowNodeType.Tool, "{\"toolName\": \"testTool\"}");

        var result = await toolNode.ExecuteAsync(step, new WorkflowContext());

        result.Success.Should().BeTrue();
        result.OutputJson.Should().Contain("ok");
    }
}
