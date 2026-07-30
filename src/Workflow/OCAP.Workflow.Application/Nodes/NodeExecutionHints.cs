using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Application.Nodes;

public static class NodeExecutionHints
{
    public const string DelaySignal = "__delay__";

    public static void RequestPause(WorkflowContext context, string signal, DateTime? untilUtc = null)
    {
        context.ShouldPause = true;
        context.WaitSignal = signal;
        context.WaitUntilUtc = untilUtc;
    }

    public static void ClearPause(WorkflowContext context)
    {
        context.ShouldPause = false;
        context.WaitSignal = null;
        context.WaitUntilUtc = null;
    }
}
