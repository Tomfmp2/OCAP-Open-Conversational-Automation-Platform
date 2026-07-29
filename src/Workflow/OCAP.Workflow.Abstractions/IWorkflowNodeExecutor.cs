using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Abstractions;

// Resultado de la ejecución individual de un nodo de Workflow.
public record NodeExecutionResult(
    bool Success,
    string NextStepId,
    string OutputJson,
    string? ErrorMessage = null
);

// Modelo de resultado de paso para mantener compatibilidad con el código existente.
public record WorkflowStepResult(
    bool Success,
    string NextStepId,
    string OutputJson,
    string? ErrorMessage = null
) : NodeExecutionResult(Success, NextStepId, OutputJson, ErrorMessage);

// Contrato agnóstico que debe implementar cada ejecutor de nodo en el motor de workflows.
public interface IWorkflowNodeExecutor
{
    WorkflowNodeType NodeType { get; }
    Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default);
}

