using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Abstractions;

// Resultado de la ejecución individual de un nodo de Workflow.
public record WorkflowStepResult(
    bool Success,
    string NextStepId,
    string OutputJson,
    string? ErrorMessage = null
);

// Contrato agnóstico que debe implementar cada tipo de nodo en el motor de workflows.
public interface IWorkflowNode
{
    WorkflowNodeType NodeType { get; }
    Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default);
}
