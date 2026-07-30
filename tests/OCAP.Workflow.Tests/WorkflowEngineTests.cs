using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Expressions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Tests;

public class WorkflowEngineTests
{
    private readonly Mock<IWorkflowDefinitionRepository> _definitionRepoMock = new();
    private readonly Mock<IWorkflowExecutionRepository> _executionRepoMock = new();
    private readonly IWorkflowExpressionEvaluator _evaluator = new WorkflowExpressionEvaluator();
    private readonly Guid _definitionId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private WorkflowDefinition _definition = null!;
    private WorkflowEngine _engine = null!;

    public WorkflowEngineTests()
    {
        var nodes = new IWorkflowNodeExecutor[]
        {
            new StartNode(),
            new EndNode(),
            new ConditionNode(_evaluator),
            new SwitchNode(_evaluator),
            new DelayNode(),
            new WaitNode(),
            new HumanApprovalNode(),
            new LoopNode(_evaluator),
            new ForEachNode(_evaluator),
            new VariableAssignNode(_evaluator),
            new MergeNode()
        };

        _definition = new WorkflowDefinition(_definitionId, _tenantId, "Test Workflow");
        _definition.AddStep(new WorkflowStep(Guid.NewGuid(), "step1", "Start", WorkflowNodeType.Start, "{}"));
        _definition.AddStep(new WorkflowStep(Guid.NewGuid(), "step2", "End", WorkflowNodeType.End, "{}"));
        _definition.AddTransition(new WorkflowTransition(Guid.NewGuid(), "step1", "step2", "true"));

        _definitionRepoMock
            .Setup(r => r.GetByIdAsync(_definitionId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_definition);

        _executionRepoMock
            .Setup(r => r.GetVariablesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowVariable>());

        _engine = CreateEngine(nodes);
    }

    private WorkflowEngine CreateEngine(IEnumerable<IWorkflowNodeExecutor> nodes)
        => new(
            new WorkflowNodeExecutorResolver(nodes),
            NullLogger<WorkflowEngine>.Instance,
            _definitionRepoMock.Object,
            _executionRepoMock.Object,
            _evaluator);

    [Fact]
    public async Task StartWorkflowAsync_ExecutesWorkflowAndCompletes()
    {
        var context = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        var execution = await _engine.StartWorkflowAsync(_definitionId, context);

        execution.Status.Should().Be(WorkflowStatus.Completed);
        execution.CompletedAtUtc.Should().NotBeNull();
        execution.WorkflowVersionNumber.Should().Be(_definition.CurrentVersion);
    }

    [Fact]
    public async Task PauseAndResumeWorkflow_ChangesStateCorrectly()
    {
        var context = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        var execution = new WorkflowExecution(Guid.NewGuid(), _definitionId, _tenantId, context.UserId, null, "step1");

        _executionRepoMock
            .Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var paused = await _engine.PauseWorkflowAsync(execution.Id);
        paused.Status.Should().Be(WorkflowStatus.Paused);

        var resumed = await _engine.ResumeWorkflowAsync(execution.Id, context);
        resumed.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public async Task CancelWorkflow_UpdatesStatusToCancelled()
    {
        var context = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        var execution = new WorkflowExecution(Guid.NewGuid(), _definitionId, _tenantId, context.UserId, null, "step1");

        _executionRepoMock
            .Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var cancelled = await _engine.CancelWorkflowAsync(execution.Id);
        cancelled.Status.Should().Be(WorkflowStatus.Cancelled);
    }

    [Fact]
    public async Task ConditionBranch_RoutesToTruePath()
    {
        var defId = Guid.NewGuid();
        var def = new WorkflowDefinition(defId, _tenantId, "Condition Flow");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "start", "Start", WorkflowNodeType.Start));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "cond", "Cond", WorkflowNodeType.Condition,
            """{"expression":"flag == true","trueStepId":"ok","falseStepId":"ko"}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "ok", "Ok", WorkflowNodeType.End));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "ko", "Ko", WorkflowNodeType.End));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "start", "cond"));

        _definitionRepoMock.Setup(r => r.GetByIdAsync(defId, _tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(def);

        var ctx = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        ctx.Variables["flag"] = true;

        var execution = await _engine.StartWorkflowAsync(defId, ctx);
        execution.Status.Should().Be(WorkflowStatus.Completed);
        ctx.Variables.Should().ContainKey("condition");
    }

    [Fact]
    public async Task WaitNode_Pauses_And_ResumeWithSignal_Continues()
    {
        var defId = Guid.NewGuid();
        var def = new WorkflowDefinition(defId, _tenantId, "Wait Flow");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "start", "Start", WorkflowNodeType.Start));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "wait", "Wait", WorkflowNodeType.Wait,
            """{"signal":"order.paid"}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "end", "End", WorkflowNodeType.End));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "start", "wait"));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "wait", "end"));

        _definitionRepoMock.Setup(r => r.GetByIdAsync(defId, _tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(def);

        WorkflowExecution? stored = null;
        _executionRepoMock
            .Setup(r => r.AddAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((e, _) => stored = e)
            .Returns(Task.CompletedTask);
        _executionRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((e, _) => stored = e)
            .Returns(Task.CompletedTask);
        _executionRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);

        var ctx = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        var paused = await _engine.StartWorkflowAsync(defId, ctx);
        paused.Status.Should().Be(WorkflowStatus.Paused);
        paused.WaitSignal.Should().Be("order.paid");

        var resumeCtx = new WorkflowContext { TenantId = _tenantId, UserId = ctx.UserId };
        var completed = await _engine.ResumeWithSignalAsync(paused.Id, _tenantId, "order.paid", """{"paid":true}""", resumeCtx);
        completed.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public async Task HumanApproval_ResumeApproved_RoutesCorrectly()
    {
        var defId = Guid.NewGuid();
        var def = new WorkflowDefinition(defId, _tenantId, "Approval");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "start", "Start", WorkflowNodeType.Start));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "approve", "Approve", WorkflowNodeType.HumanApproval,
            """{"signal":"approval","approveStepId":"yes","rejectStepId":"no"}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "yes", "Yes", WorkflowNodeType.End));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "no", "No", WorkflowNodeType.End));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "start", "approve"));

        _definitionRepoMock.Setup(r => r.GetByIdAsync(defId, _tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(def);

        WorkflowExecution? stored = null;
        _executionRepoMock.Setup(r => r.AddAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((e, _) => stored = e).Returns(Task.CompletedTask);
        _executionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((e, _) => stored = e).Returns(Task.CompletedTask);
        _executionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);

        var paused = await _engine.StartWorkflowAsync(defId, new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() });
        paused.Status.Should().Be(WorkflowStatus.Paused);

        var done = await _engine.ResumeWithSignalAsync(paused.Id, _tenantId, "approved", null,
            new WorkflowContext { TenantId = _tenantId, UserId = paused.UserId });
        done.Status.Should().Be(WorkflowStatus.Completed);
        done.CurrentStepId.Should().Be("yes");
    }

    [Fact]
    public async Task Retry_OnTransientFailure_EventuallySucceeds()
    {
        var flaky = new FlakyNode(failuresBeforeSuccess: 2);
        var engine = CreateEngine([new StartNode(), flaky, new EndNode()]);

        var defId = Guid.NewGuid();
        var def = new WorkflowDefinition(defId, _tenantId, "Retry");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "start", "Start", WorkflowNodeType.Start));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "flaky", "Flaky", WorkflowNodeType.Script,
            """{"retryCount":3,"retryDelayMs":1,"retryOnFailure":true}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "end", "End", WorkflowNodeType.End));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "start", "flaky"));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "flaky", "end"));

        _definitionRepoMock.Setup(r => r.GetByIdAsync(defId, _tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(def);

        var result = await engine.StartWorkflowAsync(defId, new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() });
        result.Status.Should().Be(WorkflowStatus.Completed);
        flaky.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task Timeout_FailsStepWhenExceeded()
    {
        var slow = new SlowNode(TimeSpan.FromSeconds(2));
        var engine = CreateEngine([new StartNode(), slow, new EndNode()]);

        var defId = Guid.NewGuid();
        var def = new WorkflowDefinition(defId, _tenantId, "Timeout");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "start", "Start", WorkflowNodeType.Start));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "slow", "Slow", WorkflowNodeType.Script,
            """{"timeoutSeconds":1,"retryOnFailure":false}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "end", "End", WorkflowNodeType.End));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "start", "slow"));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "slow", "end"));

        _definitionRepoMock.Setup(r => r.GetByIdAsync(defId, _tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(def);

        var result = await engine.StartWorkflowAsync(defId, new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() });
        result.Status.Should().Be(WorkflowStatus.Failed);
        result.ErrorMessage.Should().Contain("Timeout");
    }

    [Fact]
    public async Task TenantIsolation_ResumeRequiresMatchingTenant()
    {
        var execution = new WorkflowExecution(Guid.NewGuid(), _definitionId, _tenantId, Guid.NewGuid(), null, "step1");
        _executionRepoMock
            .Setup(r => r.GetByIdAsync(execution.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, Guid tenant, CancellationToken _) => tenant == _tenantId ? execution : null);

        var act = () => _engine.ResumeWorkflowAsync(execution.Id, new WorkflowContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ForEach_IteratesCollection()
    {
        var defId = Guid.NewGuid();
        var def = new WorkflowDefinition(defId, _tenantId, "ForEach");
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "start", "Start", WorkflowNodeType.Start));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "each", "Each", WorkflowNodeType.ForEach,
            """{"itemsVariable":"items","itemVariable":"item","indexVariable":"index","bodyStepId":"body","exitStepId":"end"}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "body", "Body", WorkflowNodeType.VariableAssign,
            """{"assignments":{"last":"{{item}}"}}"""));
        def.AddStep(new WorkflowStep(Guid.NewGuid(), "end", "End", WorkflowNodeType.End));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "start", "each"));
        def.AddTransition(new WorkflowTransition(Guid.NewGuid(), "body", "each"));

        _definitionRepoMock.Setup(r => r.GetByIdAsync(defId, _tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(def);

        var ctx = new WorkflowContext { TenantId = _tenantId, UserId = Guid.NewGuid() };
        ctx.Variables["items"] = new object[] { "a", "b", "c" };

        var result = await _engine.StartWorkflowAsync(defId, ctx);
        result.Status.Should().Be(WorkflowStatus.Completed);
        ctx.Variables["last"].ToString().Should().Be("c");
    }

    private sealed class FlakyNode : IWorkflowNodeExecutor
    {
        private readonly int _failuresBeforeSuccess;
        public int Attempts { get; private set; }
        public WorkflowNodeType NodeType => WorkflowNodeType.Script;
        public FlakyNode(int failuresBeforeSuccess) => _failuresBeforeSuccess = failuresBeforeSuccess;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
        {
            Attempts++;
            if (Attempts <= _failuresBeforeSuccess)
                return Task.FromResult(new NodeExecutionResult(false, string.Empty, "{}", "transient"));
            return Task.FromResult(new NodeExecutionResult(true, "next", "{\"ok\":true}"));
        }
    }

    private sealed class SlowNode : IWorkflowNodeExecutor
    {
        private readonly TimeSpan _delay;
        public WorkflowNodeType NodeType => WorkflowNodeType.Script;
        public SlowNode(TimeSpan delay) => _delay = delay;

        public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return new NodeExecutionResult(true, "next", "{\"ok\":true}");
        }
    }
}
