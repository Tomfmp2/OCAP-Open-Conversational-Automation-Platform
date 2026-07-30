using Microsoft.Extensions.DependencyInjection;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Infrastructure.Repositories;
using OCAP.Workflow.Infrastructure.Services;

namespace OCAP.Workflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowDefinitionRepository, EfWorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowExecutionRepository, EfWorkflowExecutionRepository>();
        services.AddScoped<IWorkflowDatabaseExecutor, WorkflowDatabaseExecutor>();
        services.AddScoped<IWorkflowEmailSender, WorkflowEmailSender>();
        services.AddSingleton<IWorkflowScheduler, EfWorkflowScheduler>();
        services.AddHostedService<WorkflowResumeHostedService>();
        return services;
    }
}
