using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;
using System.Text.Json;
using OCAP.Core.Events;

namespace OCAP.Workflow.Application.Services;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowNodeExecutorResolver _nodeResolver;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly IWorkflowDefinitionRepository _definitionRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IEventBus? _eventBus;

    public WorkflowEngine(
        IWorkflowNodeExecutorResolver nodeResolver, 
        ILogger<WorkflowEngine> logger,
        IWorkflowDefinitionRepository definitionRepository,
        IWorkflowExecutionRepository executionRepository,
        IEventBus? eventBus = null)
    {
        _nodeResolver = nodeResolver ?? throw new ArgumentNullException(nameof(nodeResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _definitionRepository = definitionRepository ?? throw new ArgumentNullException(nameof(definitionRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _eventBus = eventBus;
    }

    public async Task<WorkflowExecution> StartWorkflowAsync(Guid workflowDefinitionId, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var definition = await _definitionRepository.GetByIdAsync(workflowDefinitionId, context.TenantId, cancellationToken);
        if (definition == null) throw new KeyNotFoundException($"WorkflowDefinition {workflowDefinitionId} no encontrada.");

        var startStep = definition.Steps.FirstOrDefault(s => s.NodeType == WorkflowNodeType.Start);
        if (startStep == null) throw new InvalidOperationException("El Workflow no tiene nodo Start.");

        var execution = new WorkflowExecution(Guid.NewGuid(), workflowDefinitionId, context.TenantId, context.UserId, context.AgentId, startStep.StepId);
        await _executionRepository.AddAsync(execution, cancellationToken);

        _logger.LogInformation("Iniciando ejecución de Workflow {ExecutionId} para Tenant {TenantId}", execution.Id, context.TenantId);

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(new WorkflowStartedEvent(execution.Id, workflowDefinitionId, context.TenantId, context.UserId, context.AgentId), cancellationToken);
        }

        return await RunExecutionLoopAsync(execution, definition, context, cancellationToken);
    }

    public async Task<WorkflowExecution> PauseWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
        if (execution == null) throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");

        execution.Pause();
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        _logger.LogInformation("Ejecución de Workflow {ExecutionId} Pausada.", executionId);
        return execution;
    }

    public async Task<WorkflowExecution> ResumeWorkflowAsync(Guid executionId, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
        if (execution == null) throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");

        var definition = await _definitionRepository.GetByIdAsync(execution.WorkflowDefinitionId, execution.TenantId, cancellationToken);
        if (definition == null) throw new KeyNotFoundException($"WorkflowDefinition {execution.WorkflowDefinitionId} no encontrada.");

        var savedVariables = await _executionRepository.GetVariablesAsync(executionId, cancellationToken);
        foreach (var v in savedVariables)
        {
            try
            {
                var val = JsonSerializer.Deserialize<object>(v.ValueJson);
                if (val != null) context.Variables[v.Key] = val;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Error al deserializar la variable {VariableKey} para la ejecución {ExecutionId}", v.Key, executionId);
            }
        }

        execution.Resume();
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        _logger.LogInformation("Reanudando ejecución de Workflow {ExecutionId}...", executionId);

        return await RunExecutionLoopAsync(execution, definition, context, cancellationToken);
    }

    public async Task<WorkflowExecution> CancelWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
        if (execution == null) throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");

        execution.Cancel();
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        _logger.LogInformation("Ejecución de Workflow {ExecutionId} Cancelada.", executionId);
        return execution;
    }

    public async Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return await _executionRepository.GetByIdAsync(executionId, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowExecutionHistory>> GetExecutionHistoryAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return await _executionRepository.GetHistoryAsync(executionId, cancellationToken);
    }

    private async Task<WorkflowExecution> RunExecutionLoopAsync(WorkflowExecution execution, WorkflowDefinition definition, WorkflowContext context, CancellationToken cancellationToken)
    {
        int maxSteps = 1000;
        int stepsExecuted = 0;

        while (execution.Status == WorkflowStatus.Running && stepsExecuted < maxSteps && !cancellationToken.IsCancellationRequested)
        {
            var currentStep = definition.Steps.FirstOrDefault(s => s.StepId == execution.CurrentStepId);
            if (currentStep == null)
            {
                execution.Fail($"El paso {execution.CurrentStepId} no existe en la definición del Workflow.");
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                break;
            }

            IWorkflowNodeExecutor? nodeExecutor = null;
            try
            {
                nodeExecutor = _nodeResolver.Resolve(currentStep.NodeType);
            }
            catch (KeyNotFoundException)
            {
                execution.Fail($"El ejecutor para el tipo de nodo {currentStep.NodeType} no está registrado.");
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                break;
            }

            var stopwatch = Stopwatch.StartNew();
            NodeExecutionResult result;
            try
            {
                result = await nodeExecutor.ExecuteAsync(currentStep, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar el paso {StepId} del Workflow {ExecutionId}", currentStep.StepId, execution.Id);
                result = new NodeExecutionResult(false, string.Empty, "{}", ex.Message);
            }
            stopwatch.Stop();

            var history = new WorkflowExecutionHistory(
                Guid.NewGuid(), execution.Id, currentStep.StepId, currentStep.Name, currentStep.NodeType.ToString(), 
                result.Success ? "Success" : "Failed", stopwatch.Elapsed.TotalMilliseconds, "{}", result.OutputJson
            );
            await _executionRepository.AddHistoryAsync(history, cancellationToken);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new NodeExecutedEvent(
                    execution.Id,
                    currentStep.StepId,
                    currentStep.Name,
                    currentStep.NodeType.ToString(),
                    result.Success,
                    stopwatch.Elapsed.TotalMilliseconds,
                    result.OutputJson,
                    result.ErrorMessage,
                    execution.TenantId
                ), cancellationToken);
            }

            if (!result.Success)
            {
                execution.Fail(result.ErrorMessage ?? "Error desconocido en el nodo.");
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                break;
            }

            // Integrar resultado al contexto (simplificado)
            try
            {
                var outputDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result.OutputJson);
                if (outputDict != null)
                {
                    foreach(var kvp in outputDict)
                        context.Variables[kvp.Key] = kvp.Value;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "El OutputJson del paso {StepId} no es un diccionario JSON válido", currentStep.StepId);
            }

            if (currentStep.NodeType == WorkflowNodeType.End)
            {
                execution.Complete(JsonSerializer.Serialize(context.Variables));
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                break;
            }
            
            // Pausar para espera o aprobación
            if (currentStep.NodeType == WorkflowNodeType.Wait || currentStep.NodeType == WorkflowNodeType.HumanApproval)
            {
                execution.Pause();
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                break;
            }

            // Encontrar la transición siguiente
            var transitions = definition.Transitions.Where(t => t.FromStepId == currentStep.StepId).ToList();
            if (transitions.Count == 0 && result.NextStepId != "end")
            {
                execution.Fail($"El paso {currentStep.StepId} no tiene transiciones definidas.");
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                break;
            }

            // Simplificado: usar el primer NextStepId que nos de el nodo, si está definido en transiciones, o usar la primera transición incondicional.
            var nextStepId = result.NextStepId;
            if (string.IsNullOrWhiteSpace(nextStepId) || nextStepId == "next")
            {
                var nextTransition = transitions.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.ConditionExpression) || t.ConditionExpression == "true");
                if (nextTransition != null) nextStepId = nextTransition.ToStepId;
                else nextStepId = transitions.FirstOrDefault()?.ToStepId;
            }
            
            if (string.IsNullOrWhiteSpace(nextStepId))
            {
                 execution.Complete(JsonSerializer.Serialize(context.Variables));
                 await _executionRepository.UpdateAsync(execution, cancellationToken);
                 break;
            }

            execution.AdvanceTo(nextStepId);
            await _executionRepository.UpdateAsync(execution, cancellationToken);
            stepsExecuted++;
        }

        if (stepsExecuted >= maxSteps)
        {
            execution.Fail("Se excedió el número máximo de iteraciones en el workflow (loop infinito?).");
            await _executionRepository.UpdateAsync(execution, cancellationToken);
        }

        var varsToSave = context.Variables.Select(kvp => 
            new WorkflowVariable(Guid.NewGuid(), execution.Id, kvp.Key, JsonSerializer.Serialize(kvp.Value))
        ).ToList();
        await _executionRepository.SetVariablesAsync(execution.Id, varsToSave, cancellationToken);

        if (_eventBus != null)
        {
            if (execution.Status == WorkflowStatus.Completed)
            {
                var duration = execution.CompletedAtUtc.HasValue
                    ? (execution.CompletedAtUtc.Value - execution.StartedAtUtc).TotalMilliseconds
                    : 0;
                await _eventBus.PublishAsync(new WorkflowCompletedEvent(
                    execution.Id,
                    execution.WorkflowDefinitionId,
                    execution.TenantId,
                    execution.OutputJson ?? "{}",
                    duration
                ), cancellationToken);
            }
            else if (execution.Status == WorkflowStatus.Failed)
            {
                await _eventBus.PublishAsync(new WorkflowFailedEvent(
                    execution.Id,
                    execution.WorkflowDefinitionId,
                    execution.TenantId,
                    execution.ErrorMessage ?? "Workflow execution failed."
                ), cancellationToken);
            }
        }

        return execution;
    }
}
