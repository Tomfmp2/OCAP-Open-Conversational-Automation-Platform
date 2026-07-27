using Microsoft.Extensions.DependencyInjection;
using OCAP.Application.UseCases;

namespace OCAP.Application.Extensions;

// Extensión para configurar servicios de la capa de aplicación
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ReceiveMessageUseCase>();
        services.AddScoped<SendResponseUseCase>();
        services.AddScoped<GetConversationHistoryUseCase>();

        return services;
    }
}
