using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Telegram.Configuration;
using OCAP.Channels.Telegram.Services;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Telegram.Extensions;

// Método de extensión para registrar todos los componentes del adaptador Telegram en el contenedor DI.
public static class TelegramServiceExtensions
{
    public static IServiceCollection AddTelegramChannel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));

        services.AddHttpClient<TelegramApiClient>();
        services.AddSingleton<TelegramWebhookValidator>();
        services.AddScoped<TelegramMessageReceiver>();
        services.AddScoped<TelegramMessageSender>();
        services.AddScoped<IChannelProvider, TelegramChannelProvider>();
        services.AddScoped<TelegramChannelProvider>();
        services.AddScoped<ITelegramBotRuntimeManager, TelegramBotRuntimeManager>();
        services.AddHostedService<TelegramWebhookRegistrationService>();
        services.AddHostedService<TelegramPollingBackgroundService>();

        return services;
    }
}
