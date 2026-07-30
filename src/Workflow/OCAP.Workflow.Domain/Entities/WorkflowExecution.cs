using System.Text.Json;
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
    public int WorkflowVersionNumber { get; private set; } = 1;
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }
    public string OutputJson { get; private set; } = "{}";
    public string? ErrorMessage { get; private set; }
    public string? WaitSignal { get; private set; }
    public DateTime? WaitUntilUtc { get; private set; }
    public string CompensationJson { get; private set; } = "[]";
    public string? ResumePayloadJson { get; private set; }

    private WorkflowExecution()
    {
        CurrentStepId = string.Empty;
    }

    public WorkflowExecution(Guid id, Guid workflowDefinitionId, Guid tenantId, Guid userId, Guid? agentId = null, string currentStepId = "start")
    {
        Id = id;
        WorkflowDefinitionId = workflowDefinitionId;
        TenantId = tenantId;
        UserId = userId;
        AgentId = agentId;
        CurrentStepId = string.IsNullOrWhiteSpace(currentStepId) ? "start" : currentStepId;
        Status = WorkflowStatus.Running;
    }

    public void AdvanceTo(string nextStepId) => CurrentStepId = nextStepId;
    public void Pause() => Status = WorkflowStatus.Paused;
    public void Resume() => Status = WorkflowStatus.Running;

    public void WaitFor(string signal, DateTime? until = null)
    {
        WaitSignal = signal ?? throw new ArgumentNullException(nameof(signal));
        WaitUntilUtc = until;
        Status = WorkflowStatus.Paused;
    }

    public void ClearWait()
    {
        WaitSignal = null;
        WaitUntilUtc = null;
    }

    public void SetVersion(int versionNumber) => WorkflowVersionNumber = versionNumber;

    public void Complete(string outputJson)
    {
        Status = WorkflowStatus.Completed;
        OutputJson = outputJson;
        CompletedAtUtc = DateTime.UtcNow;
        ClearWait();
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
        ClearWait();
    }

    public void SetResumePayload(string? payloadJson) => ResumePayloadJson = payloadJson;

    public void PushCompensation(string stepId)
    {
        var stack = GetCompensationStack().ToList();
        stack.Add(stepId);
        CompensationJson = JsonSerializer.Serialize(stack);
    }

    public IReadOnlyList<string> GetCompensationStack()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(CompensationJson) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    public void ClearCompensation() => CompensationJson = "[]";
}
