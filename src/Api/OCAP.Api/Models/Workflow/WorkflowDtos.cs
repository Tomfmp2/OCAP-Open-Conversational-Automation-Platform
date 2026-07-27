namespace OCAP.Api.Models.Workflow;

// DTO para solicitar la creación o edición de un Workflow.
public class CreateWorkflowRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";
}

// DTO para representar el resumen o definición de un Workflow.
public record WorkflowDefinitionDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    int CurrentVersion,
    string Status,
    int StepCount,
    DateTime CreatedAtUtc
);

// DTO para representar la ejecución de un Workflow.
public record WorkflowExecutionDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    Guid TenantId,
    Guid UserId,
    Guid? AgentId,
    string CurrentStepId,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string OutputJson,
    string? ErrorMessage
);

// DTO para posición de un nodo en el canvas del Visual Builder.
public class NodePositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

// DTO para representar un nodo visual en el Visual Builder.
public class VisualNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Icon { get; set; } = "bi-gear";
    public string Color { get; set; } = "#3B82F6";
    public NodePositionDto Position { get; set; } = new();
    public string ConfigurationJson { get; set; } = "{}";
    public List<string> InputPorts { get; set; } = new() { "input" };
    public List<string> OutputPorts { get; set; } = new() { "output" };
}

// DTO para representar una conexión visual (edge) en el Visual Builder.
public class VisualEdgeDto
{
    public string Id { get; set; } = string.Empty;
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string FromPort { get; set; } = "output";
    public string ToPort { get; set; } = "input";
    public string ConditionExpression { get; set; } = string.Empty;
}

// DTO para el esquema completo del Visual Workflow Builder.
public class VisualWorkflowGraphDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public List<VisualNodeDto> Nodes { get; set; } = new();
    public List<VisualEdgeDto> Edges { get; set; } = new();
}

// DTO para plantillas pre-construidas de Workflow.
public class WorkflowTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public VisualWorkflowGraphDto Graph { get; set; } = new();
}

// DTO para solicitar simulación paso a paso de un Workflow.
public class WorkflowSimulationRequestDto
{
    public VisualWorkflowGraphDto Graph { get; set; } = new();
    public string InitialContextJson { get; set; } = "{}";
}

// DTO para el resultado de un paso en la simulación.
public class WorkflowSimulationStepResultDto
{
    public string NodeId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public double DurationMs { get; set; }
    public string OutputJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }
}

// DTO para la respuesta completa de simulación.
public class WorkflowSimulationResponseDto
{
    public bool Success { get; set; }
    public double TotalDurationMs { get; set; }
    public List<WorkflowSimulationStepResultDto> ExecutionSteps { get; set; } = new();
    public string FinalOutputJson { get; set; } = "{}";
}
