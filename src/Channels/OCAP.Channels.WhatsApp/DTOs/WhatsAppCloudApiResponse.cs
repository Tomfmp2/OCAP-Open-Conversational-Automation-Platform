using System.Text.Json.Serialization;

namespace OCAP.Channels.WhatsApp.DTOs;

public class WhatsAppCloudApiResponse
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = string.Empty;

    [JsonPropertyName("contacts")]
    public List<WhatsAppCloudResponseContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WhatsAppCloudResponseMessage>? Messages { get; set; }
    
    [JsonPropertyName("error")]
    public WhatsAppCloudApiError? Error { get; set; }
}

public class WhatsAppCloudResponseContact
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("wa_id")]
    public string WaId { get; set; } = string.Empty;
}

public class WhatsAppCloudResponseMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class WhatsAppCloudApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("fbtrace_id")]
    public string FbTraceId { get; set; } = string.Empty;
}
