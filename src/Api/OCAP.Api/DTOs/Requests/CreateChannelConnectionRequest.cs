namespace OCAP.Api.DTOs.Requests;

// Solicitud para registrar una nueva conexión de canal en el sistema.
public class CreateChannelConnectionRequest
{
    public string Provider { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Credentials { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}
