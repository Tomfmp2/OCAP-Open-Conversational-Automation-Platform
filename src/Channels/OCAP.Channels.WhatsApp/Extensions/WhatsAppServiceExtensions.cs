using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Channels.WhatsApp.Extensions;

// Extensión para registrar el canal de WhatsApp Evolution API en el contenedor de Inyección de Dependencias.
public static class WhatsAppServiceExtensions
{
    // Registra las opciones, cliente HTTP, validador de webhooks y servicios del canal de WhatsApp.
    public static IServiceCollection AddWhatsAppChannel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WhatsAppSettings>(configuration.GetSection(WhatsAppSettings.SectionName));

        // Registrar HttpClient para EvolutionApiClient
        services.AddHttpClient<EvolutionApiClient>();

        services.AddScoped<WhatsAppWebhookValidator>();
        services.AddScoped<WhatsAppMessageReceiver>();
        services.AddScoped<WhatsAppMessageSender>();
        services.AddScoped<WhatsAppChannelProvider>();

        return services;
    }
}
