using Microsoft.Extensions.DependencyInjection;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Application.Services;

namespace OCAP.Workflow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton<IWorkflowEngine, WorkflowEngine>();
        services.AddSingleton<IWorkflowValidator, WorkflowValidator>();
        services.AddSingleton<IWorkflowDesignerMapper, WorkflowDesignerMapper>();
        services.AddSingleton<IWorkflowNodeExecutorResolver, WorkflowNodeExecutorResolver>();

        // Node Executors estándar
        services.AddSingleton<IWorkflowNodeExecutor, StartNode>();
        services.AddSingleton<IWorkflowNodeExecutor, EndNode>();
        services.AddSingleton<IWorkflowNodeExecutor, ConditionNode>();
        services.AddSingleton<IWorkflowNodeExecutor, LLMNode>();
        services.AddSingleton<IWorkflowNodeExecutor, ToolNode>();
        services.AddSingleton<IWorkflowNodeExecutor, DelayNode>();
        services.AddSingleton<IWorkflowNodeExecutor, WaitNode>();
        services.AddSingleton<IWorkflowNodeExecutor, HumanApprovalNode>();
        services.AddSingleton<IWorkflowNodeExecutor, LoopNode>();
        services.AddSingleton<IWorkflowNodeExecutor, SwitchNode>();
        services.AddSingleton<IWorkflowNodeExecutor, ParallelNode>();
        services.AddSingleton<IWorkflowNodeExecutor, MergeNode>();
        services.AddSingleton<IWorkflowNodeExecutor, WebhookNode>();
        services.AddSingleton<IWorkflowNodeExecutor, HttpRequestNodeExecutor>();
        services.AddSingleton<IWorkflowNodeExecutor, ScriptNode>();
        services.AddSingleton<IWorkflowNodeExecutor, SubWorkflowNode>();
        services.AddSingleton<IWorkflowNodeExecutor, ErrorHandlerNode>();

        // Knowledge Node Executors
        services.AddSingleton<IWorkflowNodeExecutor, KnowledgeSearchNode>();
        services.AddSingleton<IWorkflowNodeExecutor, SemanticSearchNode>();
        services.AddSingleton<IWorkflowNodeExecutor, RetrieveContextNode>();
        services.AddSingleton<IWorkflowNodeExecutor, AskKnowledgeBaseNode>();
        services.AddSingleton<IWorkflowNodeExecutor, DocumentUploadNode>();
        services.AddSingleton<IWorkflowNodeExecutor, ReindexNode>();

        return services;
    }
}
