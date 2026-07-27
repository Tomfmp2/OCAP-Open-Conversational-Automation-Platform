using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Abstractions;

// Contrato principal del motor de automatización y orquestación de Workflows.
public interface IWorkflowEngine
{
    Task<WorkflowExecution> StartWorkflowAsync(Guid workflowDefinitionId, WorkflowContext context, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> PauseWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> ResumeWorkflowAsync(Guid executionId, WorkflowContext context, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> CancelWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionHistory>> GetExecutionHistoryAsync(Guid executionId, CancellationToken cancellationToken = default);
}
