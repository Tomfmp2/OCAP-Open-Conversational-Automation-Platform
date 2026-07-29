using Microsoft.Extensions.DependencyInjection;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Infrastructure.Repositories;

namespace OCAP.Workflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowDefinitionRepository, EfWorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowExecutionRepository, EfWorkflowExecutionRepository>();
        return services;
    }
}
