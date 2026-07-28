namespace OCAP.Channels.Abstractions.Registry;

// DTO con información técnica del catálogo de proveedores de canales disponibles en OCAP.
public class AvailableChannelProviderInfo
{
    public string Provider { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresOAuth { get; set; }
    public List<string> SupportedFeatures { get; set; } = new();
}

// Contrato del registro global de proveedores de canales soportados en tiempo de ejecución.
public interface IChannelRegistry
{
    void RegisterProvider(AvailableChannelProviderInfo providerInfo);
    IEnumerable<AvailableChannelProviderInfo> GetAvailableProviders();
    AvailableChannelProviderInfo? ResolveProvider(string providerName);
}
