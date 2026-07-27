namespace OCAP.Api.Models.Workflow;

// DTO para listar definiciones de Workflow.
public record WorkflowDefinitionDto(Guid Id, Guid TenantId, string Name, string Description, int CurrentVersion, string Status, int StepsCount, DateTime CreatedAtUtc);

// Petición de creación o edición de un Workflow.
public record CreateWorkflowRequestDto(string Name, string Description, string DefinitionJson);

// DTO de estado de ejecución de un Workflow.
public record WorkflowExecutionDto(Guid Id, Guid WorkflowDefinitionId, Guid TenantId, Guid UserId, Guid? AgentId, string CurrentStepId, string Status, DateTime StartedAtUtc, DateTime? CompletedAtUtc, string OutputJson, string? ErrorMessage);

// DTO de historial paso a paso de ejecución.
public record WorkflowExecutionHistoryDto(Guid Id, Guid ExecutionId, string StepId, string StepName, string NodeType, string Status, double DurationMs, string InputJson, string OutputJson, string? ErrorMessage, DateTime ExecutedAtUtc);
