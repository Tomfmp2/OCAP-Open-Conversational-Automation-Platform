using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Channels.Abstractions.Configuration;

namespace OCAP.Channels.Abstractions.Extensions;

// Extensión de registro de servicios de canales para la inyección de dependencias de .NET.
public static class ChannelServiceExtensions
{
    // Enlaza la sección de configuración de canales desde appsettings.
    public static IServiceCollection AddChannels(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChannelsSettings>(configuration.GetSection(ChannelsSettings.SectionName));
        return services;
    }
}
