using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Abstractions;

public interface IWorkflowNodeExecutorResolver
{
    IWorkflowNodeExecutor Resolve(WorkflowNodeType nodeType);
}
