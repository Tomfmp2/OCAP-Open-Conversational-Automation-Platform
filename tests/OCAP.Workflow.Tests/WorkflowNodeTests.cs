using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Expressions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;
using OCAP.Workflow.Infrastructure.Services;

namespace OCAP.Workflow.Tests;

public class WorkflowNodeTests
{
    private readonly IWorkflowExpressionEvaluator _evaluator = new WorkflowExpressionEvaluator();

    [Fact]
    public async Task StartNode_ReturnsSuccessStepResult()
    {
        var result = await new StartNode().ExecuteAsync(
            new WorkflowStep(Guid.NewGuid(), "start_1", "Inicio", WorkflowNodeType.Start),
            new WorkflowContext());

        result.Success.Should().BeTrue();
        result.NextStepId.Should().Be("next");
    }

    [Fact]
    public async Task EndNode_ReturnsCompletedResult()
    {
        var result = await new EndNode().ExecuteAsync(
            new WorkflowStep(Guid.NewGuid(), "end_1", "Fin", WorkflowNodeType.End),
            new WorkflowContext());

        result.Success.Should().BeTrue();
        result.NextStepId.Should().Be("end");
    }

    [Theory]
    [InlineData("status == \"open\"", true)]
    [InlineData("total > 10", true)]
    [InlineData("total < 5", false)]
    [InlineData("flag && total >= 10", true)]
    [InlineData("contains(name, \"CAP\")", true)]
    [InlineData("isEmpty(missing)", true)]
    public void ExpressionEvaluator_EvaluatesBoolExpressions(string expression, bool expected)
    {
        var vars = new Dictionary<string, object>
        {
            ["status"] = "open",
            ["total"] = 15,
            ["flag"] = true,
            ["name"] = "OCAP"
        };

        _evaluator.EvaluateBool(expression, vars).Should().Be(expected);
    }

    [Fact]
    public void ExpressionEvaluator_InterpolatesTemplates()
    {
        var vars = new Dictionary<string, object> { ["user"] = "Ana", ["count"] = 3 };
        _evaluator.Interpolate("Hola {{user}}, items={{count}}", vars).Should().Be("Hola Ana, items=3");
    }

    [Fact]
    public async Task SwitchNode_SelectsMatchingCase()
    {
        var node = new SwitchNode(_evaluator);
        var step = new WorkflowStep(Guid.NewGuid(), "sw", "Switch", WorkflowNodeType.Switch,
            """{"expression":"env","cases":{"prod":"p","default":"d"}}""");
        var ctx = new WorkflowContext();
        ctx.Variables["env"] = "prod";

        var result = await node.ExecuteAsync(step, ctx);
        result.NextStepId.Should().Be("p");
    }

    [Fact]
    public async Task VariableAssign_SetsInterpolatedValues()
    {
        var node = new VariableAssignNode(_evaluator);
        var step = new WorkflowStep(Guid.NewGuid(), "set", "Set", WorkflowNodeType.VariableAssign,
            """{"assignments":{"greeting":"Hi {{name}}","n":"{{n}}"}}""");
        var ctx = new WorkflowContext();
        ctx.Variables["name"] = "Tom";
        ctx.Variables["n"] = 7;

        var result = await node.ExecuteAsync(step, ctx);
        result.Success.Should().BeTrue();
        ctx.Variables["greeting"].ToString().Should().Be("Hi Tom");
    }

    [Fact]
    public async Task DelayNode_ShortDelay_DoesNotPause()
    {
        var node = new DelayNode();
        var step = new WorkflowStep(Guid.NewGuid(), "d", "Delay", WorkflowNodeType.Delay, """{"delayMs":5}""");
        var ctx = new WorkflowContext();

        var result = await node.ExecuteAsync(step, ctx);
        result.Success.Should().BeTrue();
        ctx.ShouldPause.Should().BeFalse();
    }

    [Fact]
    public async Task DelayNode_LongDelay_RequestsPause()
    {
        var node = new DelayNode();
        var step = new WorkflowStep(Guid.NewGuid(), "d", "Delay", WorkflowNodeType.Delay, """{"delaySeconds":60}""");
        var ctx = new WorkflowContext();

        var result = await node.ExecuteAsync(step, ctx);
        result.Success.Should().BeTrue();
        ctx.ShouldPause.Should().BeTrue();
        ctx.WaitSignal.Should().Be(NodeExecutionHints.DelaySignal);
        ctx.WaitUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseExecutor_RejectsNonSelect()
    {
        var act = () => WorkflowDatabaseExecutor.ValidateSql("DELETE FROM Users");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DatabaseExecutor_AllowsSelect()
    {
        var act = () => WorkflowDatabaseExecutor.ValidateSql("SELECT Id FROM Users WHERE TenantId = @tenantId");
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ParallelNode_RunsBranchesConcurrently()
    {
        var resolver = new WorkflowNodeExecutorResolver(
        [
            new VariableAssignNode(_evaluator),
            new StartNode()
        ]);

        using var services = new ServiceCollection()
            .AddSingleton<IWorkflowNodeExecutorResolver>(resolver)
            .BuildServiceProvider();
        var node = new ParallelNode(services, NullLogger<ParallelNode>.Instance);
        var def = new WorkflowDefinition(Guid.NewGuid(), Guid.NewGuid(), "P");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "a", "A", WorkflowNodeType.VariableAssign,
            """{"assignments":{"a":"1"}}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "b", "B", WorkflowNodeType.VariableAssign,
            """{"assignments":{"b":"2"}}"""));

        var step = new WorkflowStep(Guid.NewGuid(), "par", "Parallel", WorkflowNodeType.Parallel,
            """{"branchStepIds":["a","b"],"joinStepId":"join","waitForAll":true}""");

        var ctx = new WorkflowContext { Definition = def };
        var result = await node.ExecuteAsync(step, ctx);

        result.Success.Should().BeTrue();
        result.NextStepId.Should().Be("join");
        ctx.Variables.Should().ContainKey("a");
        ctx.Variables.Should().ContainKey("b");
    }
}
