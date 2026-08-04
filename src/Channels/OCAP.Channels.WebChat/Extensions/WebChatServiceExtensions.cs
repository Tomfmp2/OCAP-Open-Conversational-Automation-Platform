using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.WebChat.Configuration;
using OCAP.Channels.WebChat.Services;

namespace OCAP.Channels.WebChat.Extensions;

public static class WebChatServiceExtensions
{
    public static IServiceCollection AddWebChatChannel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebChatOptions>(configuration.GetSection(WebChatOptions.SectionName));
        services.AddScoped<WebChatMessageReceiver>();
        services.AddScoped<WebChatMessageSender>();
        services.AddScoped<WebChatChannelProvider>();
        services.AddScoped<IChannelProvider, WebChatChannelProvider>();
        services.AddScoped<IWebChatRuntimeManager, WebChatRuntimeManager>();
        return services;
    }
}
