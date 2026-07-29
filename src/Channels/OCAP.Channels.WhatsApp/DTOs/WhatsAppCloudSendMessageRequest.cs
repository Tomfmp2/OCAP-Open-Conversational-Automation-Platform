using System.Text.Json.Serialization;

namespace OCAP.Channels.WhatsApp.DTOs;

public class WhatsAppCloudSendMessageRequest
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = "whatsapp";

    [JsonPropertyName("recipient_type")]
    public string RecipientType { get; set; } = "individual";

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public WhatsAppCloudText? Text { get; set; }
}

public class WhatsAppCloudText
{
    [JsonPropertyName("preview_url")]
    public bool PreviewUrl { get; set; } = false;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
