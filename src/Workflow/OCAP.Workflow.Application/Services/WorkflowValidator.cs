using System.Collections.Generic;
using System.Linq;
using OCAP.Workflow.Designer.DTOs;
using OCAP.Workflow.Designer.Models;

namespace OCAP.Workflow.Application.Services;

public class WorkflowValidator : IWorkflowValidator
{
    public WorkflowValidationResult Validate(VisualWorkflowGraph graph)
    {
        var errors = new List<WorkflowValidationError>();
        var warnings = new List<WorkflowValidationWarning>();

        if (graph == null)
        {
            errors.Add(new WorkflowValidationError(string.Empty, "Workflow graph is null"));
            return new WorkflowValidationResult(false, errors, warnings);
        }

        // Validate Nodes
        if (graph.Nodes == null || !graph.Nodes.Any())
        {
            errors.Add(new WorkflowValidationError(string.Empty, "Workflow must have at least one node"));
        }
        else
        {
            // Must have a start node
            var startNodes = graph.Nodes.Where(n => n.Type.Equals("start", System.StringComparison.OrdinalIgnoreCase)).ToList();
            if (!startNodes.Any())
            {
                errors.Add(new WorkflowValidationError(string.Empty, "Workflow must have exactly one 'start' node"));
            }
            else if (startNodes.Count > 1)
            {
                errors.Add(new WorkflowValidationError(string.Empty, "Workflow cannot have multiple 'start' nodes"));
            }

            foreach (var node in graph.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    errors.Add(new WorkflowValidationError(node.Id ?? "unknown", "Node ID cannot be empty"));
                }
                
                if (string.IsNullOrWhiteSpace(node.StepId))
                {
                    errors.Add(new WorkflowValidationError(node.Id ?? "unknown", $"Node {node.Name} must have a StepId"));
                }
            }

            // Detect cycles and unreachable nodes
            ValidateGraphStructure(graph, errors, warnings, startNodes.FirstOrDefault());
        }

        return new WorkflowValidationResult(errors.Count == 0, errors, warnings);
    }

    private void ValidateGraphStructure(VisualWorkflowGraph graph, List<WorkflowValidationError> errors, List<WorkflowValidationWarning> warnings, VisualNode? startNode)
    {
        if (startNode == null) return;

        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        bool HasCycle(string currentId)
        {
            visited.Add(currentId);
            recursionStack.Add(currentId);

            var outgoingEdges = graph.Edges?.Where(e => e.FromNodeId == currentId) ?? Enumerable.Empty<VisualEdge>();
            foreach (var edge in outgoingEdges)
            {
                if (!visited.Contains(edge.ToNodeId))
                {
                    if (HasCycle(edge.ToNodeId))
                        return true;
                }
                else if (recursionStack.Contains(edge.ToNodeId))
                {
                    return true;
                }
            }

            recursionStack.Remove(currentId);
            return false;
        }

        if (HasCycle(startNode.Id))
        {
            errors.Add(new WorkflowValidationError(string.Empty, "Workflow contains cycles, which are not allowed"));
        }

        // Check for unreachable nodes
        var unreachableNodes = graph.Nodes.Where(n => !visited.Contains(n.Id)).ToList();
        foreach (var node in unreachableNodes)
        {
            warnings.Add(new WorkflowValidationWarning(node.Id, $"Node {node.Name} is unreachable"));
        }
    }
}
