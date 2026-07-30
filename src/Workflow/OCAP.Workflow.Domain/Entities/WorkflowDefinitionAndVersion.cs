using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Domain.Entities;

// Entidad que representa la definición declarativa de un proceso o Workflow.
public class WorkflowDefinition
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int CurrentVersion { get; private set; } = 1;
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;
    public List<WorkflowStep> Steps { get; private set; } = new();
    public List<WorkflowTransition> Transitions { get; private set; } = new();
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public WorkflowDefinition(Guid id, Guid tenantId, string name, string description = "")
    {
        Id = id;
        TenantId = tenantId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
    }

    public void UpdateDetails(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
    }

    public void AddStep(WorkflowStep step) => Steps.Add(step);
    public void AddTransition(WorkflowTransition transition) => Transitions.Add(transition);
    public void Activate() => Status = WorkflowStatus.Active;

    public WorkflowVersion PublishVersion(string definitionJson)
    {
        CurrentVersion++;
        return new WorkflowVersion(
            Guid.NewGuid(),
            Id,
            CurrentVersion,
            definitionJson ?? "{}",
            TenantId);
    }
}

// Entidad que representa una versión inmutable del diseño de un Workflow.
public class WorkflowVersion
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public int VersionNumber { get; private set; }
    public string DefinitionJson { get; private set; } = "{}";
    public DateTime PublishedAtUtc { get; private set; } = DateTime.UtcNow;

    public WorkflowVersion(Guid id, Guid workflowDefinitionId, int versionNumber, string definitionJson, Guid tenantId = default)
    {
        Id = id;
        TenantId = tenantId;
        WorkflowDefinitionId = workflowDefinitionId;
        VersionNumber = versionNumber;
        DefinitionJson = definitionJson ?? "{}";
    }
}
