using OCAP.Workflow.Designer.Layout;
using OCAP.Workflow.Designer.Models;

namespace OCAP.Workflow.Designer.DTOs;

public record WorkflowDesignerSaveRequest(
    Guid WorkflowDefinitionId,
    VisualWorkflowGraph Graph,
    LayoutState Layout
);

public record WorkflowDesignerLoadResponse(
    Guid WorkflowDefinitionId,
    VisualWorkflowGraph Graph,
    LayoutState Layout
);

public record WorkflowValidationWarning(string NodeId, string Message);
public record WorkflowValidationError(string NodeId, string Message);

public record WorkflowValidationResult(
    bool IsValid,
    List<WorkflowValidationError> Errors,
    List<WorkflowValidationWarning> Warnings
);
