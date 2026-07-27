using System;
using OCAP.Workflow.Designer.Models;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Services;

public class WorkflowDesignerMapper : IWorkflowDesignerMapper
{
    public WorkflowDefinition MapToDomain(VisualWorkflowGraph graph, Guid tenantId)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));

        var workflowId = Guid.TryParse(graph.Id, out var parsedId) ? parsedId : Guid.NewGuid();
        var workflow = new WorkflowDefinition(workflowId, tenantId, graph.Name, graph.Description);

        foreach (var node in graph.Nodes)
        {
            var nodeType = MapNodeType(node.Type);
            var step = new WorkflowStep(
                Guid.NewGuid(), 
                node.Id, 
                node.Name, 
                nodeType, 
                node.ConfigurationJson);
            
            workflow.AddStep(step);
        }

        foreach (var edge in graph.Edges)
        {
            var transition = new WorkflowTransition(
                Guid.NewGuid(),
                edge.FromNodeId,
                edge.ToNodeId,
                "" // Conditions can be mapped if edge contains condition logic
            );
            
            workflow.AddTransition(transition);
        }

        return workflow;
    }

    private WorkflowNodeType MapNodeType(string designerNodeType)
    {
        return designerNodeType.ToLowerInvariant() switch
        {
            "start" => WorkflowNodeType.Start,
            "end" => WorkflowNodeType.End,
            "llm" => WorkflowNodeType.LLM,
            "http" => WorkflowNodeType.ApiRequest,
            "condition" => WorkflowNodeType.Condition,
            "script" => WorkflowNodeType.Script,
            _ => WorkflowNodeType.Tool // Default fallback
        };
    }
}
