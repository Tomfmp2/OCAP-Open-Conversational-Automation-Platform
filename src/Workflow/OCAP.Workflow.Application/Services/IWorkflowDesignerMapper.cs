using System;
using OCAP.Workflow.Designer.Models;
using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Application.Services;

public interface IWorkflowDesignerMapper
{
    WorkflowDefinition MapToDomain(VisualWorkflowGraph graph, Guid tenantId);
    VisualWorkflowGraph MapFromDomain(WorkflowDefinition definition);
}
