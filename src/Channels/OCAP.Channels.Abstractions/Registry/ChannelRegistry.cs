using System.Collections.Concurrent;

namespace OCAP.Channels.Abstractions.Registry;

// Registro thread-safe en tiempo de ejecución para el descubrimiento y resolución de proveedores de canales.
public class ChannelRegistry : IChannelRegistry
{
    private readonly ConcurrentDictionary<string, AvailableChannelProviderInfo> _providers = new(StringComparer.OrdinalIgnoreCase);

    public ChannelRegistry()
    {
        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "Telegram",
            DisplayName = "Telegram Bot API",
            Description = "Conexión nativa con Telegram Bot API con validación de secreto de webhook.",
            RequiresOAuth = false,
            IsImplemented = true,
            SupportedFeatures = new List<string> { "Text", "Webhooks", "BotApi" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "WhatsApp",
            DisplayName = "WhatsApp Enterprise Adapter",
            Description = "Canal de mensajería empresarial con WhatsApp Cloud API y webhooks.",
            RequiresOAuth = false,
            IsImplemented = true,
            SupportedFeatures = new List<string> { "Text", "Media", "Webhooks" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "WebChat",
            DisplayName = "OCAP WebChat Widget",
            Description = "Canal embebible para portales web con respuesta síncrona del Enterprise Assistant.",
            RequiresOAuth = false,
            IsImplemented = true,
            SupportedFeatures = new List<string> { "Text", "RealTime", "Widget" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "GoogleWorkspace",
            DisplayName = "Google Workspace & Gmail",
            Description = "Integración OAuth para servicios Google (próximamente como canal de mensajería).",
            RequiresOAuth = true,
            IsImplemented = false,
            SupportedFeatures = new List<string> { "OAuth2", "Email", "Calendar", "Drive" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "Slack",
            DisplayName = "Slack Workspaces",
            Description = "Integración con Slack Bot API — pendiente de adaptador runtime.",
            RequiresOAuth = true,
            IsImplemented = false,
            SupportedFeatures = new List<string> { "Text", "Interactive", "Webhooks" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "MicrosoftTeams",
            DisplayName = "Microsoft Teams Bot",
            Description = "Integración con Azure Bot Framework — pendiente de adaptador runtime.",
            RequiresOAuth = true,
            IsImplemented = false,
            SupportedFeatures = new List<string> { "Text", "Cards", "Webhooks" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "Discord",
            DisplayName = "Discord Bot",
            Description = "Adaptador Discord — pendiente de implementacion.",
            RequiresOAuth = false,
            IsImplemented = false,
            SupportedFeatures = new List<string> { "Text", "Webhooks" }
        });
    }

    public void RegisterProvider(AvailableChannelProviderInfo providerInfo)
    {
        if (providerInfo == null || string.IsNullOrWhiteSpace(providerInfo.Provider)) return;
        _providers[providerInfo.Provider] = providerInfo;
    }

    public IEnumerable<AvailableChannelProviderInfo> GetAvailableProviders()
    {
        return _providers.Values.ToList();
    }

    public AvailableChannelProviderInfo? ResolveProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName)) return null;
        _providers.TryGetValue(providerName.Trim(), out var providerInfo);
        return providerInfo;
    }
}
