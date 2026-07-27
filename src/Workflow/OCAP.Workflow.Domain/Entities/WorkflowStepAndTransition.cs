using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Domain.Entities;

// Entidad que representa un paso o nodo dentro del diseño de un Workflow.
public class WorkflowStep
{
    public Guid Id { get; private set; }
    public string StepId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WorkflowNodeType NodeType { get; private set; }
    public string ConfigurationJson { get; private set; } = "{}";

    public WorkflowStep(Guid id, string stepId, string name, WorkflowNodeType nodeType, string configurationJson = "{}")
    {
        Id = id;
        StepId = string.IsNullOrWhiteSpace(stepId) ? Guid.NewGuid().ToString("N") : stepId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        NodeType = nodeType;
        ConfigurationJson = configurationJson ?? "{}";
    }
}

// Entidad que representa una transición o conexión dirigida entre dos pasos del Workflow.
public class WorkflowTransition
{
    public Guid Id { get; private set; }
    public string FromStepId { get; private set; } = string.Empty;
    public string ToStepId { get; private set; } = string.Empty;
    public string ConditionExpression { get; private set; } = string.Empty;

    public WorkflowTransition(Guid id, string fromStepId, string toStepId, string conditionExpression = "")
    {
        Id = id;
        FromStepId = fromStepId ?? throw new ArgumentNullException(nameof(fromStepId));
        ToStepId = toStepId ?? throw new ArgumentNullException(nameof(toStepId));
        ConditionExpression = conditionExpression ?? string.Empty;
    }
}
