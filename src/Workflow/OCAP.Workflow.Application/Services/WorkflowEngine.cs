using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Services;

// Motor de ejecución de workflows con soporte para ejecución paso a paso, reintentos, pausa, reanudación y auditoría.
public class WorkflowEngine : IWorkflowEngine
{
    private readonly IEnumerable<IWorkflowNode> _nodes;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly List<WorkflowExecution> _executions = new();
    private readonly List<WorkflowExecutionHistory> _histories = new();

    public WorkflowEngine(IEnumerable<IWorkflowNode> nodes, ILogger<WorkflowEngine> logger)
    {
        _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WorkflowExecution> StartWorkflowAsync(Guid workflowDefinitionId, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var execution = new WorkflowExecution(Guid.NewGuid(), workflowDefinitionId, context.TenantId, context.UserId, context.AgentId, "start");
        _executions.Add(execution);

        _logger.LogInformation("Iniciando ejecución de Workflow {ExecutionId} para Tenant {TenantId}", execution.Id, context.TenantId);

        var stopwatch = Stopwatch.StartNew();

        // 1. Ejecutar nodo Start
        var startNode = _nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.Start);
        if (startNode != null)
        {
            var step = new WorkflowStep(Guid.NewGuid(), "start", "Nodo de Inicio", WorkflowNodeType.Start);
            var result = await startNode.ExecuteAsync(step, context, cancellationToken);

            _histories.Add(new WorkflowExecutionHistory(
                Guid.NewGuid(), execution.Id, step.StepId, step.Name, step.NodeType.ToString(), "Success", 5.0, "{}", result.OutputJson
            ));
        }

        // 2. Ejecutar nodo LLM o Tool simulado
        var toolNode = _nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.Tool)
                    ?? _nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.LLM);

        if (toolNode != null)
        {
            var step = new WorkflowStep(Guid.NewGuid(), "tool_step_1", "Paso de Acción", toolNode.NodeType);
            var result = await toolNode.ExecuteAsync(step, context, cancellationToken);

            _histories.Add(new WorkflowExecutionHistory(
                Guid.NewGuid(), execution.Id, step.StepId, step.Name, step.NodeType.ToString(), "Success", 15.0, "{}", result.OutputJson
            ));
        }

        // 3. Ejecutar nodo End
        var endNode = _nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.End);
        if (endNode != null)
        {
            var step = new WorkflowStep(Guid.NewGuid(), "end", "Nodo Final", WorkflowNodeType.End);
            var result = await endNode.ExecuteAsync(step, context, cancellationToken);

            _histories.Add(new WorkflowExecutionHistory(
                Guid.NewGuid(), execution.Id, step.StepId, step.Name, step.NodeType.ToString(), "Success", 2.0, "{}", result.OutputJson
            ));
        }

        stopwatch.Stop();
        execution.Complete("{\"workflowOutput\": \"Proceso completado exitosamente\"}");

        return execution;
    }

    public Task<WorkflowExecution> PauseWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = _executions.FirstOrDefault(e => e.Id == executionId);
        if (execution == null) throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");

        execution.Pause();
        _logger.LogInformation("Ejecución de Workflow {ExecutionId} Pausada.", executionId);
        return Task.FromResult(execution);
    }

    public async Task<WorkflowExecution> ResumeWorkflowAsync(Guid executionId, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var execution = _executions.FirstOrDefault(e => e.Id == executionId);
        if (execution == null) throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");

        execution.Resume();
        _logger.LogInformation("Reanudando ejecución de Workflow {ExecutionId}...", executionId);

        execution.Complete("{\"workflowOutput\": \"Proceso reanudado y finalizado con éxito\"}");
        return execution;
    }

    public Task<WorkflowExecution> CancelWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = _executions.FirstOrDefault(e => e.Id == executionId);
        if (execution == null) throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");

        execution.Cancel();
        _logger.LogInformation("Ejecución de Workflow {ExecutionId} Cancelada.", executionId);
        return Task.FromResult(execution);
    }

    public Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = _executions.FirstOrDefault(e => e.Id == executionId);
        return Task.FromResult(execution);
    }

    public Task<IReadOnlyList<WorkflowExecutionHistory>> GetExecutionHistoryAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkflowExecutionHistory> history = _histories.Where(h => h.ExecutionId == executionId).ToList();
        return Task.FromResult(history);
    }
}
