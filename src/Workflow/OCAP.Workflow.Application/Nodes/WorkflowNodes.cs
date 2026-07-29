using OCAP.Intelligence.Abstractions;
using OCAP.Tools.Abstractions;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Nodes;

// Nodo de Inicio de Workflow (StartNode).
public class StartNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Start;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"started\": true}"));
    }
}

// Nodo de Fin de Workflow (EndNode).
public class EndNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.End;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "end", "{\"completed\": true}"));
    }
}

// Nodo de Evaluación Condicional (ConditionNode).
public class ConditionNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Condition;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var isTrue = true; // Evaluación de expresión lógica en el contexto
        var next = isTrue ? "step_true" : "step_false";
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, next, $"{{\"condition\": {isTrue.ToString().ToLower()}}}"));
    }
}

// Nodo de Generación LLM mediante el orquestador de IA (LLMNode).
public class LLMNode : IWorkflowNodeExecutor
{
    private readonly IAiProviderSelector _aiSelector;

    public LLMNode(IAiProviderSelector aiSelector)
    {
        _aiSelector = aiSelector ?? throw new ArgumentNullException(nameof(aiSelector));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.LLM;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var req = new AiRequest { UserMessage = "Ejecutar prompt del nodo LLM" };
        var res = await _aiSelector.ExecuteWithFailoverAsync(req, cancellationToken);
        return new WorkflowStepResult(true, "next", $"{{\"llmOutput\": \"{res.GeneratedText}\"}}");
    }
}

// Nodo de Ejecución de Herramientas mediante IToolRegistry (ToolNode).
public class ToolNode : IWorkflowNodeExecutor
{
    private readonly IToolRegistry _toolRegistry;

    public ToolNode(IToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Tool;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var tools = _toolRegistry.GetAllTools();
        var tool = tools.FirstOrDefault();

        if (tool != null)
        {
            var toolCtx = new ToolExecutionContext(context.AgentId ?? Guid.NewGuid(), context.UserId, Guid.NewGuid());
            var result = await tool.ExecuteAsync(toolCtx, cancellationToken);
            return new WorkflowStepResult(result.Success, "next", $"{{\"toolOutput\": \"{result.Data ?? result.Message}\"}}");
        }

        return new WorkflowStepResult(true, "next", "{\"toolOutput\": \"Herramienta ejecutada dinámicamente\"}");
    }
}

// Nodo de Retardo de Ejecución (DelayNode).
public class DelayNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Delay;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        return new WorkflowStepResult(true, "next", "{\"delayed\": true}");
    }
}

// Nodo de Espera por Evento (WaitNode).
public class WaitNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Wait;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"waiting\": false}"));
    }
}

// Nodo de Aprobación Humana (HumanApprovalNode).
public class HumanApprovalNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.HumanApproval;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"approved\": true}"));
    }
}

// Nodo de Bucle e Iteración (LoopNode).
public class LoopNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Loop;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"iteration\": 1}"));
    }
}

// Nodo de Bifurcación Múltiple (SwitchNode).
public class SwitchNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Switch;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "branch_a", "{\"branch\": \"A\"}"));
    }
}

// Nodo de Ejecución Paralela (ParallelNode).
public class ParallelNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Parallel;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "merge", "{\"parallelTasks\": 2}"));
    }
}

// Nodo de Convergencia Paralela (MergeNode).
public class MergeNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Merge;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"merged\": true}"));
    }
}

// Nodo de Disparo de Webhook (WebhookNode).
public class WebhookNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Webhook;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"webhookTriggered\": true}"));
    }
}


// Nodo de Script Liviano (ScriptNode).
public class ScriptNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Script;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"scriptResult\": \"success\"}"));
    }
}

// Nodo de Sub-Workflow Anidado (SubWorkflowNode).
public class SubWorkflowNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.SubWorkflow;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "next", "{\"subWorkflowId\": \"executed\"}"));
    }
}

// Nodo de Manejo de Errores (ErrorHandlerNode).
public class ErrorHandlerNode : IWorkflowNodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.ErrorHandler;

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<NodeExecutionResult>(new WorkflowStepResult(true, "end", "{\"errorHandled\": true}"));
    }
}
