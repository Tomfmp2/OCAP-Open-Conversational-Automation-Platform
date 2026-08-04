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

        var workflowId = Guid.TryParse(graph.Id, out var parsedId) && parsedId != Guid.Empty
            ? parsedId
            : Guid.NewGuid();
        var workflow = new WorkflowDefinition(workflowId, tenantId, graph.Name, graph.Description);

        foreach (var node in graph.Nodes)
        {
            var nodeType = MapNodeType(node.Type);
            var step = new WorkflowStep(
                Guid.NewGuid(),
                string.IsNullOrWhiteSpace(node.StepId) ? node.Id : node.StepId,
                node.Name,
                nodeType,
                string.IsNullOrWhiteSpace(node.ConfigurationJson) ? "{}" : node.ConfigurationJson);

            workflow.AddStep(step);
        }

        foreach (var edge in graph.Edges)
        {
            var transition = new WorkflowTransition(
                Guid.NewGuid(),
                edge.FromNodeId,
                edge.ToNodeId,
                edge.ConditionExpression ?? string.Empty);

            workflow.AddTransition(transition);
        }

        return workflow;
    }

    public VisualWorkflowGraph MapFromDomain(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var graph = new VisualWorkflowGraph
        {
            Id = definition.Id.ToString(),
            Name = definition.Name,
            Description = definition.Description,
            Version = definition.CurrentVersion
        };

        var index = 0;
        foreach (var step in definition.Steps)
        {
            graph.Nodes.Add(new VisualNode
            {
                Id = step.StepId,
                StepId = step.StepId,
                Name = step.Name,
                Type = MapNodeTypeToDesigner(step.NodeType),
                Category = step.NodeType.ToString(),
                Position = new NodePosition(80 + (index % 4) * 180, 80 + (index / 4) * 120),
                ConfigurationJson = step.ConfigurationJson
            });
            index++;
        }

        var edgeIndex = 0;
        foreach (var transition in definition.Transitions)
        {
            graph.Edges.Add(new VisualEdge
            {
                Id = $"edge-{edgeIndex++}",
                FromNodeId = transition.FromStepId,
                ToNodeId = transition.ToStepId,
                ConditionExpression = transition.ConditionExpression
            });
        }

        return graph;
    }

    private static WorkflowNodeType MapNodeType(string designerNodeType)
    {
        return designerNodeType.ToLowerInvariant() switch
        {
            "start" or "trigger" => WorkflowNodeType.Start,
            "end" => WorkflowNodeType.End,
            "llm" or "agent" => WorkflowNodeType.LLM,
            "http" or "action" => WorkflowNodeType.ApiRequest,
            "condition" => WorkflowNodeType.Condition,
            "script" => WorkflowNodeType.Script,
            "tool" => WorkflowNodeType.Tool,
            "delay" => WorkflowNodeType.Delay,
            "webhook" => WorkflowNodeType.Webhook,
            _ => WorkflowNodeType.Tool
        };
    }

    private static string MapNodeTypeToDesigner(WorkflowNodeType nodeType) => nodeType switch
    {
        WorkflowNodeType.Start => "start",
        WorkflowNodeType.End => "end",
        WorkflowNodeType.LLM => "llm",
        WorkflowNodeType.Condition => "condition",
        WorkflowNodeType.ApiRequest => "http",
        WorkflowNodeType.Script => "script",
        WorkflowNodeType.Tool => "tool",
        WorkflowNodeType.Delay => "delay",
        WorkflowNodeType.Webhook => "webhook",
        _ => nodeType.ToString().ToLowerInvariant()
    };
}
