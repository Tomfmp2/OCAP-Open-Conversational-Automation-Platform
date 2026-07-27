using Microsoft.Extensions.DependencyInjection;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Application.UseCases;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Extensions;

// Extensión para registrar los servicios del Agent Engine en Inyección de Dependencias.
public static class AgentServiceExtensions
{
    // Registra los resolutores, despachadores, registro de herramientas y casos de uso del motor de agentes.
    public static IServiceCollection AddAgentEngineServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddScoped<IIntentResolver, RuleBasedIntentResolver>();
        services.AddScoped<IActionDispatcher, ActionDispatcher>();
        services.AddScoped<ProcessAgentMessageUseCase>();

        return services;
    }
}
