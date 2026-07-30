using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Abstractions;

// Contrato principal del motor de automatización y orquestación de Workflows.
public interface IWorkflowEngine
{
    Task<WorkflowExecution> StartWorkflowAsync(Guid workflowDefinitionId, WorkflowContext context, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> PauseWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> ResumeWorkflowAsync(Guid executionId, WorkflowContext context, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> ResumeWithSignalAsync(Guid executionId, Guid tenantId, string signal, string? payloadJson, WorkflowContext context, CancellationToken cancellationToken = default);
    Task<WorkflowExecution> CancelWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecutionHistory>> GetExecutionHistoryAsync(Guid executionId, CancellationToken cancellationToken = default);
}

public static class WorkflowEngineExtensions
{
    public static Task<WorkflowExecution> SignalAsync(
        this IWorkflowEngine engine,
        Guid executionId,
        Guid tenantId,
        string signal,
        string? payloadJson,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
        => engine.ResumeWithSignalAsync(executionId, tenantId, signal, payloadJson, context, cancellationToken);
}
