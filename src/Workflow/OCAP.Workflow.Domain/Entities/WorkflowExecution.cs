using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Domain.Entities;

// Aggregate Root que encapsula la ejecución y estado de una instancia de Workflow.
public class WorkflowExecution
{
    public Guid Id { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? AgentId { get; private set; }
    public string CurrentStepId { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Running;
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }
    public string OutputJson { get; private set; } = "{}";
    public string? ErrorMessage { get; private set; }

    public WorkflowExecution(Guid id, Guid workflowDefinitionId, Guid tenantId, Guid userId, Guid? agentId = null, string startStepId = "start")
    {
        Id = id;
        WorkflowDefinitionId = workflowDefinitionId;
        TenantId = tenantId;
        UserId = userId;
        AgentId = agentId;
        CurrentStepId = startStepId;
        Status = WorkflowStatus.Running;
    }

    public void AdvanceTo(string nextStepId) => CurrentStepId = nextStepId;
    public void Pause() => Status = WorkflowStatus.Paused;
    public void Resume() => Status = WorkflowStatus.Running;
    public void Complete(string outputJson)
    {
        Status = WorkflowStatus.Completed;
        OutputJson = outputJson;
        CompletedAtUtc = DateTime.UtcNow;
    }
    public void Fail(string errorMessage)
    {
        Status = WorkflowStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAtUtc = DateTime.UtcNow;
    }
    public void Cancel()
    {
        Status = WorkflowStatus.Cancelled;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
