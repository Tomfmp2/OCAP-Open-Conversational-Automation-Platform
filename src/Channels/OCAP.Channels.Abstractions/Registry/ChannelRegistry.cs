using System.Collections.Concurrent;

namespace OCAP.Channels.Abstractions.Registry;

// Registro thread-safe en tiempo de ejecución para el descubrimiento y resolución de proveedores de canales.
public class ChannelRegistry : IChannelRegistry
{
    private readonly ConcurrentDictionary<string, AvailableChannelProviderInfo> _providers = new(StringComparer.OrdinalIgnoreCase);

    public ChannelRegistry()
    {
        // Registrar proveedores estándar soportados por la plataforma por defecto.
        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "Telegram",
            DisplayName = "Telegram Bot API",
            Description = "Conexión nativa con Telegram Bot API con validación de secreto de webhook.",
            RequiresOAuth = false,
            SupportedFeatures = new List<string> { "Text", "Webhooks", "BotApi" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "WhatsApp",
            DisplayName = "WhatsApp Enterprise Adapter",
            Description = "Canal de mensajería empresarial con soporte de código QR y webhooks.",
            RequiresOAuth = false,
            SupportedFeatures = new List<string> { "Text", "Media", "Webhooks", "QRCode" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "GoogleWorkspace",
            DisplayName = "Google Workspace & Gmail",
            Description = "Integración empresarial obligatoria con Google OAuth para servicios y mensajería.",
            RequiresOAuth = true,
            SupportedFeatures = new List<string> { "OAuth2", "Email", "Calendar", "Drive" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "WebChat",
            DisplayName = "OCAP WebChat Widget",
            Description = "Canal directo embebible para portales y aplicaciones web.",
            RequiresOAuth = false,
            SupportedFeatures = new List<string> { "Text", "RealTime", "WebSockets" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "Slack",
            DisplayName = "Slack Workspaces",
            Description = "Integración con Slack Bot API para entornos corporativos.",
            RequiresOAuth = true,
            SupportedFeatures = new List<string> { "Text", "Interactive", "Webhooks" }
        });

        RegisterProvider(new AvailableChannelProviderInfo
        {
            Provider = "MicrosoftTeams",
            DisplayName = "Microsoft Teams Bot",
            Description = "Integración con Azure Bot Framework para Microsoft Teams.",
            RequiresOAuth = true,
            SupportedFeatures = new List<string> { "Text", "Cards", "Webhooks" }
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
