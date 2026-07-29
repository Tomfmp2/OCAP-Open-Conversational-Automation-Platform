using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Services;

public class WorkflowNodeExecutorResolver : IWorkflowNodeExecutorResolver
{
    private readonly Dictionary<WorkflowNodeType, IWorkflowNodeExecutor> _executors;

    public WorkflowNodeExecutorResolver(IEnumerable<IWorkflowNodeExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(executors);
        _executors = executors
            .GroupBy(e => e.NodeType)
            .ToDictionary(g => g.Key, g => g.Last());
    }

    public IWorkflowNodeExecutor Resolve(WorkflowNodeType nodeType)
    {
        if (!_executors.TryGetValue(nodeType, out var executor))
        {
            throw new KeyNotFoundException($"No executor registered for node type: {nodeType}");
        }
        return executor;
    }
}

