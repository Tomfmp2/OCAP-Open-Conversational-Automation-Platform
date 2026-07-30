using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Intelligence.Abstractions;
using OCAP.Tools.Abstractions;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Expressions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Nodes;

public class StartNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Start;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"started\": true}"));
    }
}

public class EndNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.End;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "end", "{\"completed\": true}"));
    }
}

public class ConditionNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public ConditionNode(IWorkflowExpressionEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Condition;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<ConditionNodeConfig>(step.ConfigurationJson);
        var isTrue = _evaluator.EvaluateBool(config.Expression ?? "false", context.Variables);
        var next = isTrue
            ? (config.TrueStepId ?? "true")
            : (config.FalseStepId ?? "false");

        var output = JsonSerializer.Serialize(new { condition = isTrue, branch = next });
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, next, output));
    }
}

public class SwitchNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public SwitchNode(IWorkflowExpressionEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Switch;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<SwitchNodeConfig>(step.ConfigurationJson);
        var value = _evaluator.Evaluate(config.Expression ?? string.Empty, context.Variables);
        var key = WorkflowExpressionEvaluator.FormatValue(value) ?? string.Empty;

        string next;
        if (config.Cases != null && config.Cases.TryGetValue(key, out var caseStep))
            next = caseStep;
        else if (config.Cases != null && config.Cases.TryGetValue("default", out var defaultStep))
            next = defaultStep;
        else
            next = "default";

        var output = JsonSerializer.Serialize(new { switchValue = key, branch = next });
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, next, output));
    }
}

public class DelayNode : IWorkflowNodeExecutor
{
    private const int MaxInlineDelayMs = 30_000;

    public WorkflowNodeType NodeType => WorkflowNodeType.Delay;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<DelayNodeConfig>(step.ConfigurationJson);
        var delayMs = config.DelayMs ?? (config.DelaySeconds.HasValue ? config.DelaySeconds.Value * 1000 : 0);

        if (delayMs <= 0)
            return new WorkflowStepResult(true, "next", "{\"delayed\": true}");

        if (delayMs <= MaxInlineDelayMs)
        {
            await Task.Delay(delayMs, cancellationToken);
            return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { delayed = true, delayMs }));
        }

        if (string.Equals(context.ResumeSignal, NodeExecutionHints.DelaySignal, StringComparison.OrdinalIgnoreCase))
        {
            NodeExecutionHints.ClearPause(context);
            context.ResumeSignal = null;
            return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { delayed = true, resumed = true, delayMs }));
        }

        NodeExecutionHints.RequestPause(context, NodeExecutionHints.DelaySignal, DateTime.UtcNow.AddMilliseconds(delayMs));
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { paused = true, delayMs, waitUntilUtc = context.WaitUntilUtc }));
    }
}

public class WaitNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Wait;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<WaitNodeConfig>(step.ConfigurationJson);
        var signal = config.Signal ?? "wait";

        if (!string.IsNullOrWhiteSpace(context.ResumeSignal) &&
            context.ResumeSignal.Equals(signal, StringComparison.OrdinalIgnoreCase))
        {
            NodeExecutionHints.ClearPause(context);
            context.ResumeSignal = null;
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
                true, "next",
                JsonSerializer.Serialize(new { waiting = false, resumed = true, signal })));
        }

        DateTime? until = config.TimeoutSeconds > 0
            ? DateTime.UtcNow.AddSeconds(config.TimeoutSeconds)
            : null;

        NodeExecutionHints.RequestPause(context, signal, until);
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
            true, "next",
            JsonSerializer.Serialize(new { waiting = true, signal, timeoutSeconds = config.TimeoutSeconds })));
    }
}

public class HumanApprovalNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.HumanApproval;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<HumanApprovalNodeConfig>(step.ConfigurationJson);
        var signal = config.Signal ?? "approval";

        if (!string.IsNullOrEmpty(context.ResumeSignal))
        {
            var approved = context.ResumeSignal.Equals("approved", StringComparison.OrdinalIgnoreCase);
            var rejected = context.ResumeSignal.Equals("rejected", StringComparison.OrdinalIgnoreCase);
            var next = approved
                ? (config.ApproveStepId ?? "approved")
                : rejected
                    ? (config.RejectStepId ?? "rejected")
                    : "next";

            var output = JsonSerializer.Serialize(new { approved, rejected, resumeSignal = context.ResumeSignal });
            NodeExecutionHints.ClearPause(context);
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, next, output));
        }

        NodeExecutionHints.RequestPause(context, signal);
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
            true, "next",
            JsonSerializer.Serialize(new { awaitingApproval = true, signal })));
    }
}

public class LoopNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public LoopNode(IWorkflowExpressionEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Loop;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<LoopNodeConfig>(step.ConfigurationJson);
        var counterKey = config.CounterVariable ?? "_loop.index";
        var maxIterations = config.MaxIterations > 0 ? config.MaxIterations : 100;

        var currentIndex = 0;
        if (context.Variables.TryGetValue(counterKey, out var idxVal))
        {
            if (idxVal is JsonElement je && je.TryGetInt32(out var ji))
                currentIndex = ji;
            else if (int.TryParse(idxVal?.ToString(), out var parsed))
                currentIndex = parsed;
        }

        if (currentIndex >= maxIterations)
        {
            var exitStep = config.ExitStepId ?? "next";
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
                true, exitStep,
                JsonSerializer.Serialize(new { loopExited = true, reason = "maxIterations", iteration = currentIndex })));
        }

        var shouldContinue = _evaluator.EvaluateBool(config.Condition ?? "false", context.Variables);
        if (!shouldContinue)
        {
            context.Variables.Remove(counterKey);
            var exitStep = config.ExitStepId ?? "next";
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
                true, exitStep,
                JsonSerializer.Serialize(new { loopExited = true, iteration = currentIndex })));
        }

        context.Variables[counterKey] = currentIndex + 1;
        var bodyStep = config.BodyStepId ?? "next";
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
            true, bodyStep,
            JsonSerializer.Serialize(new { loopIteration = currentIndex, continueLoop = true })));
    }
}

public class ForEachNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public ForEachNode(IWorkflowExpressionEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.ForEach;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<ForEachNodeConfig>(step.ConfigurationJson);
        var itemsKey = config.ItemsVariable ?? "items";
        var itemKey = config.ItemVariable ?? "item";
        var indexKey = config.IndexVariable ?? "index";
        var maxIterations = config.MaxIterations > 0 ? config.MaxIterations : 1000;

        var items = WorkflowExpressionEvaluator.ResolvePath(itemsKey, context.Variables);
        var count = GetCollectionCount(items);

        var currentIndex = 0;
        if (context.Variables.TryGetValue(indexKey, out var idxVal))
        {
            if (idxVal is JsonElement je && je.TryGetInt32(out var ji))
                currentIndex = ji;
            else if (int.TryParse(idxVal?.ToString(), out var parsed))
                currentIndex = parsed;
        }

        if (currentIndex >= count || currentIndex >= maxIterations)
        {
            context.Variables.Remove(indexKey);
            context.Variables.Remove(itemKey);
            var exitStep = config.ExitStepId ?? "next";
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
                true, exitStep,
                JsonSerializer.Serialize(new { forEachCompleted = true, totalItems = count, iteration = currentIndex })));
        }

        var currentItem = GetCollectionItem(items, currentIndex);
        context.Variables[itemKey] = currentItem ?? null!;
        context.Variables[indexKey] = currentIndex + 1;

        var bodyStep = config.BodyStepId ?? "next";
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(
            true, bodyStep,
            JsonSerializer.Serialize(new { forEachIndex = currentIndex, totalItems = count })));
    }

    private static int GetCollectionCount(object? items) => items switch
    {
        null => 0,
        JsonElement { ValueKind: JsonValueKind.Array } arr => arr.GetArrayLength(),
        IList<object> list => list.Count,
        Array array => array.Length,
        IEnumerable<object> enumerable => enumerable.Count(),
        _ => 0
    };

    private static object? GetCollectionItem(object? items, int index) => items switch
    {
        JsonElement { ValueKind: JsonValueKind.Array } arr when index >= 0 && index < arr.GetArrayLength()
            => WorkflowExpressionEvaluator.Unwrap(arr[index]),
        IList<object> list when index >= 0 && index < list.Count => list[index],
        Array array when index >= 0 && index < array.Length => array.GetValue(index),
        IEnumerable<object> enumerable => enumerable.ElementAtOrDefault(index),
        _ => null
    };
}

public class ParallelNode : IWorkflowNodeExecutor
{
    private readonly Lazy<IWorkflowNodeExecutorResolver> _resolver;
    private readonly ILogger<ParallelNode> _logger;

    public ParallelNode(IServiceProvider serviceProvider, ILogger<ParallelNode> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _resolver = new Lazy<IWorkflowNodeExecutorResolver>(
            () => serviceProvider.GetRequiredService<IWorkflowNodeExecutorResolver>());
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Parallel;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<ParallelNodeConfig>(step.ConfigurationJson);
        var branchIds = config.BranchStepIds ?? Array.Empty<string>();
        var definition = context.Definition;

        if (definition == null || branchIds.Length == 0)
        {
            var joinStep = config.JoinStepId ?? "next";
            return new WorkflowStepResult(true, joinStep, "{\"parallelResults\": {}}");
        }

        var results = new ConcurrentDictionary<string, object>();
        var mergedVariables = new ConcurrentDictionary<string, object>();
        var tasks = branchIds.Select(async branchId =>
        {
            var branchStep = definition.Steps.FirstOrDefault(s => s.StepId == branchId);
            if (branchStep == null)
            {
                _logger.LogWarning("Paso de rama paralela {BranchId} no encontrado.", branchId);
                return;
            }

            try
            {
                var executor = _resolver.Value.Resolve(branchStep.NodeType);
                var branchContext = CloneContext(context);
                var result = await executor.ExecuteAsync(branchStep, branchContext, cancellationToken);
                results[branchId] = new { result.Success, result.OutputJson, result.ErrorMessage };
                foreach (var kvp in branchContext.Variables)
                    mergedVariables[kvp.Key] = kvp.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando rama paralela {BranchId}", branchId);
                results[branchId] = new { Success = false, OutputJson = "{}", ErrorMessage = ex.Message };
            }
        });

        if (config.WaitForAll)
            await Task.WhenAll(tasks);
        else
            await Task.WhenAny(tasks);

        foreach (var kvp in mergedVariables)
            context.Variables[kvp.Key] = kvp.Value;

        context.Variables["parallelResults"] = results.ToDictionary(k => k.Key, v => v.Value);
        var next = config.JoinStepId ?? "next";
        return new WorkflowStepResult(true, next, JsonSerializer.Serialize(new { parallelResults = results, branchCount = branchIds.Length }));
    }

    private static WorkflowContext CloneContext(WorkflowContext source) => new()
    {
        TenantId = source.TenantId,
        UserId = source.UserId,
        AgentId = source.AgentId,
        ExecutionId = source.ExecutionId,
        Definition = source.Definition,
        Variables = new Dictionary<string, object>(source.Variables)
    };
}

public class MergeNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Merge;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        context.Variables.TryGetValue("parallelResults", out var parallelResults);
        var output = JsonSerializer.Serialize(new { merged = true, parallelResults });
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", output));
    }
}

public class LLMNode : IWorkflowNodeExecutor
{
    private readonly IAiProviderSelector _aiSelector;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public LLMNode(IAiProviderSelector aiSelector, IWorkflowExpressionEvaluator evaluator)
    {
        _aiSelector = aiSelector ?? throw new ArgumentNullException(nameof(aiSelector));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.LLM;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<LlmNodeConfig>(step.ConfigurationJson);
        var prompt = _evaluator.Interpolate(config.Prompt ?? string.Empty, context.Variables);
        var system = _evaluator.Interpolate(config.System ?? string.Empty, context.Variables);

        var req = new AiRequest
        {
            UserMessage = prompt,
            SystemInstructions = system,
            Temperature = config.Temperature > 0 ? config.Temperature : 0.2
        };

        var res = await _aiSelector.ExecuteWithFailoverAsync(req, cancellationToken);
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { llmOutput = res.GeneratedText }));
    }
}

public class ToolNode : IWorkflowNodeExecutor
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public ToolNode(IToolRegistry toolRegistry, IWorkflowExpressionEvaluator evaluator)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Tool;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<ToolNodeConfig>(step.ConfigurationJson);
        var toolName = _evaluator.Interpolate(config.ToolName ?? string.Empty, context.Variables);

        var tool = _toolRegistry.GetTool(toolName);

        if (tool == null)
        {
            return new WorkflowStepResult(false, string.Empty, "{}",
                $"Herramienta '{toolName}' no encontrada en el registro.");
        }

        var toolCtx = new ToolExecutionContext(context.AgentId ?? Guid.NewGuid(), context.UserId, context.ExecutionId != Guid.Empty ? context.ExecutionId : Guid.NewGuid());
        var result = await tool.ExecuteAsync(toolCtx, cancellationToken);
        return new WorkflowStepResult(result.Success, "next",
            JsonSerializer.Serialize(new { toolOutput = result.Data ?? result.Message, toolName }));
    }
}

public class WebhookNode : IWorkflowNodeExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorkflowExpressionEvaluator _evaluator;
    private readonly ILogger<WebhookNode> _logger;

    public WebhookNode(IHttpClientFactory httpClientFactory, IWorkflowExpressionEvaluator evaluator, ILogger<WebhookNode> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Webhook;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<WebhookNodeConfig>(step.ConfigurationJson);
        var url = _evaluator.Interpolate(config.Url ?? string.Empty, context.Variables);
        var body = _evaluator.Interpolate(config.Body ?? "{}", context.Variables);

        if (string.IsNullOrWhiteSpace(url))
            return new WorkflowStepResult(false, string.Empty, "{}", "URL del webhook no configurada.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            if (config.Headers != null)
            {
                foreach (var header in config.Headers)
                    request.Headers.TryAddWithoutValidation(header.Key, _evaluator.Interpolate(header.Value, context.Variables));
            }

            var client = _httpClientFactory.CreateClient("WebhookNode");
            var response = await client.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var output = JsonSerializer.Serialize(new
            {
                statusCode = (int)response.StatusCode,
                isSuccessStatusCode = response.IsSuccessStatusCode,
                body = responseContent
            });

            if (!response.IsSuccessStatusCode && config.FailOnErrorCode)
                return new WorkflowStepResult(false, string.Empty, output, $"Webhook fall? con c?digo {(int)response.StatusCode}.");

            return new WorkflowStepResult(true, "next", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar WebhookNode para paso {StepId}", step.StepId);
            return new WorkflowStepResult(false, string.Empty, JsonSerializer.Serialize(new { error = ex.Message }), ex.Message);
        }
    }
}

public class ScriptNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public ScriptNode(IWorkflowExpressionEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Script;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return VariableAssignHelper.ApplyAssignments(step, context, _evaluator, "scriptResult");
    }
}

public class VariableAssignNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public VariableAssignNode(IWorkflowExpressionEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.VariableAssign;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return VariableAssignHelper.ApplyAssignments(step, context, _evaluator, "assigned");
    }
}

internal static class VariableAssignHelper
{
    public static Task<NodeExecutionResult> ApplyAssignments(
        WorkflowStep step,
        WorkflowContext context,
        IWorkflowExpressionEvaluator evaluator,
        string outputKey)
    {
        var config = NodeConfiguration.Deserialize<VariableAssignNodeConfig>(step.ConfigurationJson);
        var assigned = new Dictionary<string, object?>();

        if (config.Assignments != null)
        {
            foreach (var (key, valueTemplate) in config.Assignments)
            {
                var resolved = ResolveAssignmentValue(valueTemplate, evaluator, context.Variables);
                context.Variables[key] = resolved ?? null!;
                assigned[key] = resolved;
            }
        }

        var output = JsonSerializer.Serialize(new Dictionary<string, object?> { [outputKey] = assigned });
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", output));
    }

    private static object? ResolveAssignmentValue(
        string valueTemplate,
        IWorkflowExpressionEvaluator evaluator,
        IDictionary<string, object> variables)
    {
        if (valueTemplate.Contains("{{"))
            return evaluator.Interpolate(valueTemplate, variables);

        if (valueTemplate is "true" or "false")
            return bool.Parse(valueTemplate);

        if (long.TryParse(valueTemplate, out var num))
            return num;

        if (valueTemplate == "null")
            return null;

        try
        {
            return evaluator.Evaluate(valueTemplate, variables);
        }
        catch
        {
            return valueTemplate;
        }
    }
}

public class SubWorkflowNode : IWorkflowNodeExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubWorkflowNode> _logger;

    public SubWorkflowNode(IServiceScopeFactory scopeFactory, ILogger<SubWorkflowNode> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.SubWorkflow;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<SubWorkflowNodeConfig>(step.ConfigurationJson);

        if (!Guid.TryParse(config.WorkflowDefinitionId, out var subWorkflowId))
        {
            return new WorkflowStepResult(false, string.Empty, "{}",
                "workflowDefinitionId inv?lido en configuraci?n de SubWorkflow.");
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();

            var subContext = new WorkflowContext
            {
                TenantId = context.TenantId,
                UserId = context.UserId,
                AgentId = context.AgentId,
                Variables = new Dictionary<string, object>(context.Variables)
            };

            var subExecution = await engine.StartWorkflowAsync(subWorkflowId, subContext, cancellationToken);
            var output = JsonSerializer.Serialize(new
            {
                subWorkflowExecutionId = subExecution.Id,
                subWorkflowStatus = subExecution.Status.ToString(),
                subWorkflowOutput = subExecution.OutputJson
            });

            return new WorkflowStepResult(subExecution.Status != WorkflowStatus.Failed, "next", output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar SubWorkflow {SubWorkflowId}", config.WorkflowDefinitionId);
            return new WorkflowStepResult(false, string.Empty,
                JsonSerializer.Serialize(new { error = ex.Message }), ex.Message);
        }
    }
}

public class ErrorHandlerNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.ErrorHandler;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<ErrorHandlerNodeConfig>(step.ConfigurationJson);
        var hasError = context.Variables.ContainsKey("lastError") || context.Variables.ContainsKey("_lastError");

        if (hasError)
        {
            var catchStep = config.CatchStepId ?? "next";
            var output = JsonSerializer.Serialize(new { errorHandled = true, lastError = context.Variables.GetValueOrDefault("lastError") ?? context.Variables.GetValueOrDefault("_lastError") });
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, catchStep, output));
        }

        if (config.Rethrow)
        {
            var errorMsg = context.Variables.GetValueOrDefault("lastError")?.ToString() ?? "Error desconocido";
            return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(false, string.Empty, "{}", errorMsg));
        }

        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"errorHandled\": false}"));
    }
}

public class AgentNode : IWorkflowNodeExecutor
{
    private readonly IAgentRuntime _agentRuntime;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public AgentNode(IAgentRuntime agentRuntime, IWorkflowExpressionEvaluator evaluator)
    {
        _agentRuntime = agentRuntime ?? throw new ArgumentNullException(nameof(agentRuntime));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Agent;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<AgentNodeConfig>(step.ConfigurationJson);

        if (!Guid.TryParse(config.AgentId, out var agentId))
            agentId = context.AgentId ?? Guid.Empty;

        if (agentId == Guid.Empty)
            return new WorkflowStepResult(false, string.Empty, "{}", "agentId no configurado.");

        var message = _evaluator.Interpolate(config.Message ?? string.Empty, context.Variables);
        var agentContext = new AgentContext(agentId, context.TenantId, context.UserId, message, context.Variables);

        var response = await _agentRuntime.ExecuteAgentAsync(agentContext, cancellationToken);
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { agentResponse = response, agentId }));
    }
}

public class DatabaseNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowDatabaseExecutor _databaseExecutor;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public DatabaseNode(IWorkflowDatabaseExecutor databaseExecutor, IWorkflowExpressionEvaluator evaluator)
    {
        _databaseExecutor = databaseExecutor ?? throw new ArgumentNullException(nameof(databaseExecutor));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Database;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<DatabaseNodeConfig>(step.ConfigurationJson);
        var sql = _evaluator.Interpolate(config.Sql ?? string.Empty, context.Variables);

        var parameters = new Dictionary<string, object?>();
        if (config.Parameters != null)
        {
            foreach (var (key, value) in config.Parameters)
                parameters[key] = _evaluator.Interpolate(value, context.Variables);
        }

        try
        {
            var rows = await _databaseExecutor.QueryAsync(context.TenantId, sql, parameters, cancellationToken);
            return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { rows, rowCount = rows.Count }));
        }
        catch (Exception ex)
        {
            return new WorkflowStepResult(false, string.Empty, JsonSerializer.Serialize(new { error = ex.Message }), ex.Message);
        }
    }
}

public class EmailNode : IWorkflowNodeExecutor
{
    private readonly IWorkflowEmailSender _emailSender;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public EmailNode(IWorkflowEmailSender emailSender, IWorkflowExpressionEvaluator evaluator)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Email;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<EmailNodeConfig>(step.ConfigurationJson);
        var to = _evaluator.Interpolate(config.To ?? string.Empty, context.Variables);
        var subject = _evaluator.Interpolate(config.Subject ?? string.Empty, context.Variables);
        var body = _evaluator.Interpolate(config.Body ?? string.Empty, context.Variables);

        if (string.IsNullOrWhiteSpace(to))
            return new WorkflowStepResult(false, string.Empty, "{}", "Destinatario de email no configurado.");

        await _emailSender.SendAsync(to, subject, body, cancellationToken);
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { sent = true, to, subject }));
    }
}

// Configuration DTOs
public class ConditionNodeConfig
{
    public string? Expression { get; set; }
    public string? TrueStepId { get; set; }
    public string? FalseStepId { get; set; }
}

public class SwitchNodeConfig
{
    public string? Expression { get; set; }
    public Dictionary<string, string>? Cases { get; set; }
}

public class DelayNodeConfig
{
    public int? DelayMs { get; set; }
    public int? DelaySeconds { get; set; }
}

public class WaitNodeConfig
{
    public string? Signal { get; set; }
    public int TimeoutSeconds { get; set; }
}

public class HumanApprovalNodeConfig
{
    public string? ApproveStepId { get; set; }
    public string? RejectStepId { get; set; }
    public string? Signal { get; set; }
}

public class LoopNodeConfig
{
    public string? Condition { get; set; }
    public string? BodyStepId { get; set; }
    public string? ExitStepId { get; set; }
    public int MaxIterations { get; set; } = 100;
    public string? CounterVariable { get; set; }
}

public class ForEachNodeConfig
{
    public string? ItemsVariable { get; set; }
    public string? ItemVariable { get; set; }
    public string? IndexVariable { get; set; }
    public string? BodyStepId { get; set; }
    public string? ExitStepId { get; set; }
    public int MaxIterations { get; set; } = 1000;
}

public class ParallelNodeConfig
{
    public string[]? BranchStepIds { get; set; }
    public string? JoinStepId { get; set; }
    public bool WaitForAll { get; set; } = true;
}

public class LlmNodeConfig
{
    public string? Prompt { get; set; }
    public string? System { get; set; }
    public double Temperature { get; set; } = 0.2;
}

public class ToolNodeConfig
{
    public string? ToolName { get; set; }
}

public class WebhookNodeConfig
{
    public string? Url { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public bool FailOnErrorCode { get; set; } = true;
}

public class VariableAssignNodeConfig
{
    public Dictionary<string, string>? Assignments { get; set; }
}

public class SubWorkflowNodeConfig
{
    public string? WorkflowDefinitionId { get; set; }
}

public class ErrorHandlerNodeConfig
{
    public string? CatchStepId { get; set; }
    public bool Rethrow { get; set; }
}

public class AgentNodeConfig
{
    public string? AgentId { get; set; }
    public string? Message { get; set; }
}

public class DatabaseNodeConfig
{
    public string? Sql { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
}

public class EmailNodeConfig
{
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}
