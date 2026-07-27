using OCAP.Workflow.Designer.DTOs;
using OCAP.Workflow.Designer.Models;

namespace OCAP.Workflow.Application.Services;

public interface IWorkflowValidator
{
    WorkflowValidationResult Validate(VisualWorkflowGraph graph);
}
