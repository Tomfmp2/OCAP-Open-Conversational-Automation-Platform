using Microsoft.Extensions.DependencyInjection;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Expressions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;

namespace OCAP.Workflow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddHttpClient("HttpRequestNode");
        services.AddHttpClient("WebhookNode");

        services.AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>();

        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IWorkflowValidator, WorkflowValidator>();
        services.AddScoped<IWorkflowDesignerMapper, WorkflowDesignerMapper>();
        services.AddScoped<IWorkflowNodeExecutorResolver, WorkflowNodeExecutorResolver>();

        // Node Executors estándar
        services.AddScoped<IWorkflowNodeExecutor, StartNode>();
        services.AddScoped<IWorkflowNodeExecutor, EndNode>();
        services.AddScoped<IWorkflowNodeExecutor, ConditionNode>();
        services.AddScoped<IWorkflowNodeExecutor, LLMNode>();
        services.AddScoped<IWorkflowNodeExecutor, ToolNode>();
        services.AddScoped<IWorkflowNodeExecutor, DelayNode>();
        services.AddScoped<IWorkflowNodeExecutor, WaitNode>();
        services.AddScoped<IWorkflowNodeExecutor, HumanApprovalNode>();
        services.AddScoped<IWorkflowNodeExecutor, LoopNode>();
        services.AddScoped<IWorkflowNodeExecutor, ForEachNode>();
        services.AddScoped<IWorkflowNodeExecutor, SwitchNode>();
        services.AddScoped<IWorkflowNodeExecutor, ParallelNode>();
        services.AddScoped<IWorkflowNodeExecutor, MergeNode>();
        services.AddScoped<IWorkflowNodeExecutor, WebhookNode>();
        services.AddScoped<IWorkflowNodeExecutor, HttpRequestNodeExecutor>();
        services.AddScoped<IWorkflowNodeExecutor, ScriptNode>();
        services.AddScoped<IWorkflowNodeExecutor, SubWorkflowNode>();
        services.AddScoped<IWorkflowNodeExecutor, ErrorHandlerNode>();
        services.AddScoped<IWorkflowNodeExecutor, AgentNode>();
        services.AddScoped<IWorkflowNodeExecutor, DatabaseNode>();
        services.AddScoped<IWorkflowNodeExecutor, EmailNode>();
        services.AddScoped<IWorkflowNodeExecutor, VariableAssignNode>();

        // Knowledge Node Executors
        services.AddScoped<IWorkflowNodeExecutor, KnowledgeSearchNode>();
        services.AddScoped<IWorkflowNodeExecutor, SemanticSearchNode>();
        services.AddScoped<IWorkflowNodeExecutor, RetrieveContextNode>();
        services.AddScoped<IWorkflowNodeExecutor, AskKnowledgeBaseNode>();
        services.AddScoped<IWorkflowNodeExecutor, DocumentUploadNode>();
        services.AddScoped<IWorkflowNodeExecutor, ReindexNode>();

        return services;
    }
}
