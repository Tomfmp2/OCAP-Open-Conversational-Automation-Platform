using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OCAP.Core.Events;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Services;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowNodeExecutorResolver _nodeResolver;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly IWorkflowDefinitionRepository _definitionRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IEventBus? _eventBus;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WorkflowEngine(
        IWorkflowNodeExecutorResolver nodeResolver,
        ILogger<WorkflowEngine> logger,
        IWorkflowDefinitionRepository definitionRepository,
        IWorkflowExecutionRepository executionRepository,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IEventBus? eventBus = null)
    {
        _nodeResolver = nodeResolver ?? throw new ArgumentNullException(nameof(nodeResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _definitionRepository = definitionRepository ?? throw new ArgumentNullException(nameof(definitionRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _eventBus = eventBus;
    }

    public async Task<WorkflowExecution> StartWorkflowAsync(Guid workflowDefinitionId, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(context));

        var definition = await _definitionRepository.GetByIdAsync(workflowDefinitionId, context.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowDefinition {workflowDefinitionId} no encontrada.");

        var startStep = definition.Steps.FirstOrDefault(s => s.NodeType == WorkflowNodeType.Start)
            ?? throw new InvalidOperationException("El Workflow no tiene nodo Start.");

        var execution = new WorkflowExecution(Guid.NewGuid(), workflowDefinitionId, context.TenantId, context.UserId, context.AgentId, startStep.StepId);
        execution.SetVersion(definition.CurrentVersion);

        context.ExecutionId = execution.Id;
        context.Definition = definition;
        context.ShouldPause = false;

        await _executionRepository.AddAsync(execution, cancellationToken);
        _logger.LogInformation("Iniciando Workflow {ExecutionId} v{Version} Tenant {TenantId}", execution.Id, definition.CurrentVersion, context.TenantId);

        if (_eventBus is not null)
        {
            await _eventBus.PublishAsync(
                new WorkflowStartedEvent(execution.Id, workflowDefinitionId, context.TenantId, context.UserId, context.AgentId),
                cancellationToken);
        }

        return await RunExecutionLoopAsync(execution, definition, context, cancellationToken);
    }

    public async Task<WorkflowExecution> PauseWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await RequireExecutionAsync(executionId, Guid.Empty, cancellationToken);
        execution.Pause();
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        return execution;
    }

    public async Task<WorkflowExecution> ResumeWorkflowAsync(Guid executionId, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required to resume a workflow.", nameof(context));

        var execution = await RequireExecutionAsync(executionId, context.TenantId, cancellationToken);
        if (execution.Status is WorkflowStatus.Completed or WorkflowStatus.Cancelled or WorkflowStatus.Failed)
            throw new InvalidOperationException($"No se puede reanudar una ejecución en estado {execution.Status}.");

        var definition = await _definitionRepository.GetByIdAsync(execution.WorkflowDefinitionId, execution.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowDefinition {execution.WorkflowDefinitionId} no encontrada.");

        await HydrateVariablesAsync(executionId, context, cancellationToken);

        context.ExecutionId = execution.Id;
        context.Definition = definition;
        context.TenantId = execution.TenantId;
        if (context.UserId == Guid.Empty) context.UserId = execution.UserId;
        context.AgentId ??= execution.AgentId;

        execution.Resume();
        execution.ClearWait();
        await _executionRepository.UpdateAsync(execution, cancellationToken);

        return await RunExecutionLoopAsync(execution, definition, context, cancellationToken);
    }

    public async Task<WorkflowExecution> ResumeWithSignalAsync(
        Guid executionId,
        Guid tenantId,
        string signal,
        string? payloadJson,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(signal)) throw new ArgumentException("Signal is required.", nameof(signal));

        context.TenantId = tenantId;
        context.ResumeSignal = signal;
        context.ResumePayloadJson = payloadJson;
        MergePayloadIntoContext(payloadJson, context);

        var execution = await RequireExecutionAsync(executionId, tenantId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(execution.WaitSignal) &&
            !IsCompatibleResumeSignal(execution.WaitSignal, signal))
        {
            throw new InvalidOperationException(
                $"Señal '{signal}' no coincide con la espera '{execution.WaitSignal}' de la ejecución {executionId}.");
        }

        // Delay resume: avanzar al siguiente paso antes de reentrar al loop.
        if (string.Equals(signal, NodeExecutionHints.DelaySignal, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(execution.WaitSignal, NodeExecutionHints.DelaySignal, StringComparison.OrdinalIgnoreCase))
        {
            var definition = await _definitionRepository.GetByIdAsync(execution.WorkflowDefinitionId, tenantId, cancellationToken)
                ?? throw new KeyNotFoundException($"WorkflowDefinition {execution.WorkflowDefinitionId} no encontrada.");

            await HydrateVariablesAsync(executionId, context, cancellationToken);
            context.Definition = definition;
            context.ExecutionId = execution.Id;

            var next = ResolveNextStepId(definition, execution.CurrentStepId, "next", context);
            if (!string.IsNullOrWhiteSpace(next))
                execution.AdvanceTo(next);

            execution.SetResumePayload(payloadJson);
            execution.ClearWait();
            execution.Resume();
            await _executionRepository.UpdateAsync(execution, cancellationToken);
            NodeExecutionHints.ClearPause(context);
            context.ResumeSignal = null;

            return await RunExecutionLoopAsync(execution, definition, context, cancellationToken);
        }

        execution.SetResumePayload(payloadJson);
        execution.ClearWait();
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        return await ResumeWorkflowAsync(executionId, context, cancellationToken);
    }

    public async Task<WorkflowExecution> CancelWorkflowAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await RequireExecutionAsync(executionId, Guid.Empty, cancellationToken);
        if (execution.Status is WorkflowStatus.Completed or WorkflowStatus.Cancelled)
            return execution;

        // Compensación best-effort en orden LIFO.
        try
        {
            await RunCompensationAsync(execution, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Compensación falló al cancelar {ExecutionId}", executionId);
        }

        execution.Cancel();
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        return execution;
    }

    public Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
        => _executionRepository.GetByIdAsync(executionId, Guid.Empty, cancellationToken);

    public Task<IReadOnlyList<WorkflowExecutionHistory>> GetExecutionHistoryAsync(Guid executionId, CancellationToken cancellationToken = default)
        => _executionRepository.GetHistoryAsync(executionId, cancellationToken);

    private async Task<WorkflowExecution> RunExecutionLoopAsync(
        WorkflowExecution execution,
        WorkflowDefinition definition,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        const int maxSteps = 1000;
        var stepsExecuted = 0;
        context.Definition = definition;
        context.ExecutionId = execution.Id;

        while (execution.Status == WorkflowStatus.Running && stepsExecuted < maxSteps && !cancellationToken.IsCancellationRequested)
        {
            var currentStep = definition.Steps.FirstOrDefault(s => s.StepId == execution.CurrentStepId);
            if (currentStep is null)
            {
                await FailAsync(execution, $"El paso {execution.CurrentStepId} no existe en la definición.", cancellationToken);
                break;
            }

            IWorkflowNodeExecutor nodeExecutor;
            try
            {
                nodeExecutor = _nodeResolver.Resolve(currentStep.NodeType);
            }
            catch (KeyNotFoundException)
            {
                await FailAsync(execution, $"Ejecutor no registrado para {currentStep.NodeType}.", cancellationToken);
                break;
            }

            var runtime = NodeConfiguration.Deserialize<StepRuntimeOptions>(currentStep.ConfigurationJson);
            NodeExecutionHints.ClearPause(context);

            var stopwatch = Stopwatch.StartNew();
            var result = await ExecuteWithRetryAndTimeoutAsync(nodeExecutor, currentStep, context, runtime, cancellationToken);
            stopwatch.Stop();

            await _executionRepository.AddHistoryAsync(new WorkflowExecutionHistory(
                Guid.NewGuid(),
                execution.Id,
                currentStep.StepId,
                currentStep.Name,
                currentStep.NodeType.ToString(),
                result.Success ? "Success" : "Failed",
                stopwatch.Elapsed.TotalMilliseconds,
                currentStep.ConfigurationJson,
                result.OutputJson,
                result.ErrorMessage,
                execution.TenantId), cancellationToken);

            if (_eventBus is not null)
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
                    execution.TenantId), cancellationToken);
            }

            MergeOutputIntoContext(result.OutputJson, context);

            if (!string.IsNullOrWhiteSpace(runtime.CompensationStepId))
                execution.PushCompensation(runtime.CompensationStepId);

            foreach (var item in context.CompensationStack)
                execution.PushCompensation(item);
            context.CompensationStack.Clear();

            if (!result.Success)
            {
                context.Variables["lastError"] = result.ErrorMessage ?? "Unknown error";
                var errorHandler = definition.Steps.FirstOrDefault(s => s.NodeType == WorkflowNodeType.ErrorHandler);
                if (errorHandler is not null && currentStep.NodeType != WorkflowNodeType.ErrorHandler)
                {
                    execution.AdvanceTo(errorHandler.StepId);
                    await _executionRepository.UpdateAsync(execution, cancellationToken);
                    stepsExecuted++;
                    continue;
                }

                try
                {
                    await RunCompensationAsync(execution, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Compensación falló tras error en {ExecutionId}", execution.Id);
                }

                await FailAsync(execution, result.ErrorMessage ?? "Error desconocido en el nodo.", cancellationToken);
                break;
            }

            if (currentStep.NodeType == WorkflowNodeType.End || result.NextStepId == "end")
            {
                execution.Complete(JsonSerializer.Serialize(context.Variables));
                await PersistVariablesAsync(execution, context, cancellationToken);
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                await PublishTerminalEventsAsync(execution, cancellationToken);
                return execution;
            }

            if (context.ShouldPause)
            {
                execution.WaitFor(context.WaitSignal ?? "__wait__", context.WaitUntilUtc);
                await PersistVariablesAsync(execution, context, cancellationToken);
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                return execution;
            }

            var nextStepId = ResolveNextStepId(definition, currentStep.StepId, result.NextStepId, context);
            if (string.IsNullOrWhiteSpace(nextStepId))
            {
                execution.Complete(JsonSerializer.Serialize(context.Variables));
                await PersistVariablesAsync(execution, context, cancellationToken);
                await _executionRepository.UpdateAsync(execution, cancellationToken);
                await PublishTerminalEventsAsync(execution, cancellationToken);
                return execution;
            }

            // Si el nodo devolvió un StepId que no es etiqueta de transición, úsalo directamente.
            if (definition.Steps.Any(s => s.StepId == nextStepId))
                execution.AdvanceTo(nextStepId);
            else
            {
                var byLabel = definition.Transitions.FirstOrDefault(t =>
                    t.FromStepId == currentStep.StepId &&
                    string.Equals(t.ConditionExpression, nextStepId, StringComparison.OrdinalIgnoreCase));
                if (byLabel is not null)
                    execution.AdvanceTo(byLabel.ToStepId);
                else if (definition.Steps.Any(s => s.StepId == result.NextStepId))
                    execution.AdvanceTo(result.NextStepId);
                else
                    execution.AdvanceTo(nextStepId);
            }

            await _executionRepository.UpdateAsync(execution, cancellationToken);
            stepsExecuted++;
        }

        if (cancellationToken.IsCancellationRequested && execution.Status == WorkflowStatus.Running)
        {
            execution.Cancel();
            await _executionRepository.UpdateAsync(execution, cancellationToken);
        }
        else if (stepsExecuted >= maxSteps && execution.Status == WorkflowStatus.Running)
        {
            await FailAsync(execution, "Se excedió el número máximo de iteraciones (posible loop infinito).", cancellationToken);
        }

        await PersistVariablesAsync(execution, context, cancellationToken);
        await PublishTerminalEventsAsync(execution, cancellationToken);
        return execution;
    }

    private async Task<NodeExecutionResult> ExecuteWithRetryAndTimeoutAsync(
        IWorkflowNodeExecutor executor,
        WorkflowStep step,
        WorkflowContext context,
        StepRuntimeOptions runtime,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, runtime.RetryCount + 1);
        var delayMs = Math.Max(0, runtime.RetryDelayMs);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (runtime.TimeoutSeconds > 0)
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(runtime.TimeoutSeconds));

                var result = await executor.ExecuteAsync(step, context, timeoutCts.Token);
                if (result.Success || attempt == maxAttempts || !runtime.RetryOnFailure)
                    return result;

                _logger.LogWarning(
                    "Paso {StepId} falló (intento {Attempt}/{Max}): {Error}. Reintentando...",
                    step.StepId, attempt, maxAttempts, result.ErrorMessage);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && runtime.TimeoutSeconds > 0)
            {
                lastException = new TimeoutException($"Timeout de {runtime.TimeoutSeconds}s en paso {step.StepId}.");
                if (attempt == maxAttempts || !runtime.RetryOnFailure)
                    return new NodeExecutionResult(false, string.Empty, "{\"errorType\":\"Timeout\"}", lastException.Message);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt == maxAttempts || !runtime.RetryOnFailure)
                    return new NodeExecutionResult(false, string.Empty, JsonSerializer.Serialize(new { error = ex.Message }), ex.Message);
            }

            if (delayMs > 0)
                await Task.Delay(delayMs * attempt, cancellationToken);
        }

        return new NodeExecutionResult(false, string.Empty, "{}", lastException?.Message ?? "Retry exhausted.");
    }

    private string? ResolveNextStepId(WorkflowDefinition definition, string fromStepId, string? preferredNext, WorkflowContext context)
    {
        var transitions = definition.Transitions.Where(t => t.FromStepId == fromStepId).ToList();

        if (!string.IsNullOrWhiteSpace(preferredNext) &&
            preferredNext is not ("next" or "end") &&
            definition.Steps.Any(s => s.StepId == preferredNext))
        {
            return preferredNext;
        }

        if (!string.IsNullOrWhiteSpace(preferredNext) && preferredNext is not ("next" or "end"))
        {
            var labeled = transitions.FirstOrDefault(t =>
                string.Equals(t.ConditionExpression, preferredNext, StringComparison.OrdinalIgnoreCase));
            if (labeled is not null) return labeled.ToStepId;
        }

        foreach (var transition in transitions)
        {
            if (string.IsNullOrWhiteSpace(transition.ConditionExpression) ||
                transition.ConditionExpression.Equals("true", StringComparison.OrdinalIgnoreCase))
                return transition.ToStepId;

            try
            {
                if (_expressionEvaluator.EvaluateBool(transition.ConditionExpression, context.Variables))
                    return transition.ToStepId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Expresión de transición inválida desde {From}: {Expr}", fromStepId, transition.ConditionExpression);
            }
        }

        return transitions.FirstOrDefault()?.ToStepId;
    }

    private async Task RunCompensationAsync(WorkflowExecution execution, CancellationToken cancellationToken)
    {
        var stack = execution.GetCompensationStack().Reverse().ToList();
        if (stack.Count == 0) return;

        var definition = await _definitionRepository.GetByIdAsync(execution.WorkflowDefinitionId, execution.TenantId, cancellationToken);
        if (definition is null) return;

        var context = new WorkflowContext
        {
            TenantId = execution.TenantId,
            UserId = execution.UserId,
            AgentId = execution.AgentId,
            ExecutionId = execution.Id,
            Definition = definition
        };
        await HydrateVariablesAsync(execution.Id, context, cancellationToken);

        foreach (var stepId in stack)
        {
            var step = definition.Steps.FirstOrDefault(s => s.StepId == stepId);
            if (step is null) continue;
            try
            {
                var executor = _nodeResolver.Resolve(step.NodeType);
                var result = await executor.ExecuteAsync(step, context, cancellationToken);
                await _executionRepository.AddHistoryAsync(new WorkflowExecutionHistory(
                    Guid.NewGuid(), execution.Id, step.StepId, step.Name, step.NodeType.ToString(),
                    result.Success ? "Compensated" : "CompensationFailed",
                    0, "{}", result.OutputJson, result.ErrorMessage, execution.TenantId), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo compensando paso {StepId} en {ExecutionId}", stepId, execution.Id);
            }
        }

        execution.ClearCompensation();
    }

    private static bool IsCompatibleResumeSignal(string waitSignal, string resumeSignal)
    {
        if (string.Equals(waitSignal, resumeSignal, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(resumeSignal, NodeExecutionHints.DelaySignal, StringComparison.OrdinalIgnoreCase))
            return true;

        // Human approval: WaitSignal is typically "approval"; resume uses approved/rejected.
        if (resumeSignal.Equals("approved", StringComparison.OrdinalIgnoreCase) ||
            resumeSignal.Equals("rejected", StringComparison.OrdinalIgnoreCase))
        {
            return waitSignal.Contains("approv", StringComparison.OrdinalIgnoreCase) ||
                   waitSignal.Equals("human", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private async Task FailAsync(WorkflowExecution execution, string message, CancellationToken cancellationToken)
    {
        execution.Fail(message);
        await _executionRepository.UpdateAsync(execution, cancellationToken);
    }

    private async Task PersistVariablesAsync(WorkflowExecution execution, WorkflowContext context, CancellationToken cancellationToken)
    {
        var vars = context.Variables.Select(kvp =>
            new WorkflowVariable(Guid.NewGuid(), execution.Id, kvp.Key, JsonSerializer.Serialize(kvp.Value), execution.TenantId)).ToList();
        await _executionRepository.SetVariablesAsync(execution.Id, vars, cancellationToken);
    }

    private async Task HydrateVariablesAsync(Guid executionId, WorkflowContext context, CancellationToken cancellationToken)
    {
        var saved = await _executionRepository.GetVariablesAsync(executionId, cancellationToken);
        foreach (var v in saved)
        {
            try
            {
                var val = JsonSerializer.Deserialize<object>(v.ValueJson);
                if (val is not null) context.Variables[v.Key] = val;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "No se pudo deserializar variable {Key}", v.Key);
            }
        }
    }

    private async Task PublishTerminalEventsAsync(WorkflowExecution execution, CancellationToken cancellationToken)
    {
        if (_eventBus is null) return;

        if (execution.Status == WorkflowStatus.Completed)
        {
            var duration = execution.CompletedAtUtc.HasValue
                ? (execution.CompletedAtUtc.Value - execution.StartedAtUtc).TotalMilliseconds
                : 0;
            await _eventBus.PublishAsync(new WorkflowCompletedEvent(
                execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.OutputJson ?? "{}", duration), cancellationToken);
        }
        else if (execution.Status == WorkflowStatus.Failed)
        {
            await _eventBus.PublishAsync(new WorkflowFailedEvent(
                execution.Id, execution.WorkflowDefinitionId, execution.TenantId, execution.ErrorMessage ?? "failed"), cancellationToken);
        }
    }

    private async Task<WorkflowExecution> RequireExecutionAsync(Guid executionId, Guid tenantId, CancellationToken cancellationToken)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, tenantId, cancellationToken);
        return execution ?? throw new KeyNotFoundException($"Ejecución {executionId} no encontrada.");
    }

    private static void MergePayloadIntoContext(string? payloadJson, WorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return;
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson, JsonOptions);
            if (payload is null) return;
            foreach (var kvp in payload)
                context.Variables[kvp.Key] = kvp.Value;
        }
        catch (JsonException)
        {
            context.Variables["resumePayload"] = payloadJson;
        }
    }

    private static void MergeOutputIntoContext(string? outputJson, WorkflowContext context)
    {
        if (string.IsNullOrWhiteSpace(outputJson)) return;
        try
        {
            var outputDict = JsonSerializer.Deserialize<Dictionary<string, object>>(outputJson, JsonOptions);
            if (outputDict is null) return;
            foreach (var kvp in outputDict)
                context.Variables[kvp.Key] = kvp.Value;
        }
        catch (JsonException)
        {
            // ignore non-object outputs
        }
    }
}

/// <summary>
/// Opciones de runtime opcionales embebidas en ConfigurationJson de cualquier paso.
/// </summary>
public sealed class StepRuntimeOptions
{
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; } = 200;
    public bool RetryOnFailure { get; set; } = true;
    public int TimeoutSeconds { get; set; }
    public string? CompensationStepId { get; set; }
}
