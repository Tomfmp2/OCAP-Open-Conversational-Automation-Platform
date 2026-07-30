namespace OCAP.Workflow.Domain.Entities;

// Entidad que representa una variable dinámica dentro del estado de un Workflow.
public class WorkflowVariable
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string ValueJson { get; private set; } = string.Empty;

    public WorkflowVariable(Guid id, Guid executionId, string key, string valueJson, Guid tenantId = default)
    {
        Id = id;
        TenantId = tenantId;
        ExecutionId = executionId;
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ValueJson = valueJson ?? "null";
    }
}

// Contexto de ejecución en tiempo de real que mantiene variables y metadatos.
public class WorkflowContext
{
    public Dictionary<string, object> Variables { get; set; } = new();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? AgentId { get; set; }
    public Guid ExecutionId { get; set; }
    public string? ResumeSignal { get; set; }
    public string? ResumePayloadJson { get; set; }
    public WorkflowDefinition? Definition { get; set; }
    public bool ShouldPause { get; set; }
    public string? WaitSignal { get; set; }
    public DateTime? WaitUntilUtc { get; set; }
    public List<string> CompensationStack { get; set; } = new();
}

// Modelo de resultado de ejecución de un paso o workflow completo.
public record WorkflowResult(
    bool Success,
    string OutputJson,
    string? ErrorMessage,
    int StepsExecutedCount,
    double TotalDurationMs
);

// Modelo de error durante la ejecución de un Workflow.
public record WorkflowError(
    string StepId,
    string NodeName,
    string Message,
    string StackTrace,
    DateTime OccurredAtUtc
);
