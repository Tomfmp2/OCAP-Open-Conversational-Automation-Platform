namespace OCAP.Api.DTOs.Responses;

// Respuesta con información de un proveedor de canal disponible en la plataforma.
public class ChannelProviderResponse
{
    public string Provider { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresOAuth { get; set; }
    public List<string> SupportedFeatures { get; set; } = new();
}
