namespace OCAP.Api.DTOs.Responses;

// DTO de respuesta seguro para conexiones de canal (JAMÁS expone credenciales ni secretos).
public class ChannelConnectionResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, string> ConfigurationMetadata { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
