using Microsoft.Extensions.DependencyInjection;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Application.UseCases;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Extensions;

// Extensión para registrar los servicios del Agent Engine en Inyección de Dependencias.
public static class AgentServiceExtensions
{
    // Registra resolutores, runtime, Enterprise Assistant Agent, proveedores de modelos y casos de uso del motor.
    public static IServiceCollection AddAgentEngineServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<IPermissionValidator, DefaultPermissionValidator>();
        services.AddScoped<IIntentResolver, RuleBasedIntentResolver>();
        services.AddScoped<IActionDispatcher, ActionDispatcher>();
        services.AddScoped<ProcessAgentMessageUseCase>();

        // CAP-03: Agent Runtime & Enterprise Assistant Agent Core
        services.AddScoped<IAgentResolver, AgentResolver>();
        services.AddScoped<IEnterpriseAssistantAgent, EnterpriseAssistantAgent>();
        services.AddScoped<IAgentRuntime, AgentRuntime>();
        services.AddScoped<ILanguageModelProviderSelector, DefaultLanguageModelProviderSelector>();
        services.AddSingleton<ILanguageModelProvider, FallbackLanguageModelProvider>();

        return services;
    }
}
