namespace OCAP.Workflow.Domain.Entities;

// Entidad de bitácora paso a paso para auditoría y observabilidad de ejecuciones de Workflow.
public class WorkflowExecutionHistory
{
    public Guid Id { get; private set; }
    public Guid ExecutionId { get; private set; }
    public string StepId { get; private set; } = string.Empty;
    public string StepName { get; private set; } = string.Empty;
    public string NodeType { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public double DurationMs { get; private set; }
    public string InputJson { get; private set; } = "{}";
    public string OutputJson { get; private set; } = "{}";
    public string? ErrorMessage { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; }

    public WorkflowExecutionHistory(
        Guid id,
        Guid executionId,
        string stepId,
        string stepName,
        string nodeType,
        string status,
        double durationMs,
        string inputJson,
        string outputJson,
        string? errorMessage = null)
    {
        Id = id;
        ExecutionId = executionId;
        StepId = stepId;
        StepName = stepName;
        NodeType = nodeType;
        Status = status;
        DurationMs = durationMs;
        InputJson = inputJson ?? "{}";
        OutputJson = outputJson ?? "{}";
        ErrorMessage = errorMessage;
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
