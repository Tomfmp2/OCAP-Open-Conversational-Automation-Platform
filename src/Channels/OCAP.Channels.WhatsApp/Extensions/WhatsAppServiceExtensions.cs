using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Channels.WhatsApp.Extensions;

public static class WhatsAppServiceExtensions
{
    public static IServiceCollection AddWhatsAppChannel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WhatsAppSettings>(opts =>
        {
            var primary = configuration.GetSection(WhatsAppSettings.SectionName).Get<WhatsAppSettings>();
            var cloud = configuration.GetSection("WhatsAppCloud").Get<WhatsAppSettings>();

            if (primary != null)
            {
                opts.Enabled = primary.Enabled;
                opts.Provider = primary.Provider;
                opts.BaseUrl = primary.BaseUrl;
                opts.Instance = primary.Instance;
                opts.ApiKey = primary.ApiKey;
                opts.WebhookSecret = primary.WebhookSecret;
                opts.WebhookUrl = primary.WebhookUrl;
                opts.ApiToken = primary.ApiToken;
                opts.AppSecret = primary.AppSecret;
                opts.WebhookVerifyToken = primary.WebhookVerifyToken;
            }

            if (cloud != null)
            {
                if (!string.IsNullOrWhiteSpace(cloud.ApiToken)) opts.ApiToken = cloud.ApiToken;
                if (!string.IsNullOrWhiteSpace(cloud.AppSecret)) opts.AppSecret = cloud.AppSecret;
                if (!string.IsNullOrWhiteSpace(cloud.WebhookVerifyToken)) opts.WebhookVerifyToken = cloud.WebhookVerifyToken;
            }

            if (string.IsNullOrWhiteSpace(opts.BaseUrl))
            {
                opts.BaseUrl = configuration["EVOLUTION_API_URL"] ?? "http://localhost:8080";
            }

            if (string.IsNullOrWhiteSpace(opts.ApiKey))
            {
                opts.ApiKey = configuration["EVOLUTION_API_KEY"] ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(opts.Provider))
            {
                opts.Provider = "Evolution";
            }

            if (string.IsNullOrWhiteSpace(opts.Instance))
            {
                opts.Instance = "ocap-main";
            }
        });

        services.AddHttpClient<WhatsAppApiClient>();
        services.AddHttpClient<EvolutionApiClient>();

        services.AddScoped<IWhatsAppRuntimeManager, WhatsAppRuntimeManager>();
        services.AddScoped<WhatsAppWebhookValidator>();
        services.AddScoped<WhatsAppMessageReceiver>();
        services.AddScoped<WhatsAppMessageSender>();
        services.AddScoped<WhatsAppChannelProvider>();

        return services;
    }
}
