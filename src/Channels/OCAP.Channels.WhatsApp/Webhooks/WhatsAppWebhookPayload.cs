using System.Text.Json.Serialization;

namespace OCAP.Channels.WhatsApp.Webhooks;

// Estructura del evento JSON de Webhook recibido desde Evolution API.
public class WhatsAppWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("instance")]
    public string Instance { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public WhatsAppWebhookData? Data { get; set; }
}

public class WhatsAppWebhookData
{
    [JsonPropertyName("key")]
    public WhatsAppMessageKey? Key { get; set; }

    [JsonPropertyName("pushName")]
    public string PushName { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public WhatsAppMessageBody? Message { get; set; }

    [JsonPropertyName("messageTimestamp")]
    public long MessageTimestamp { get; set; }
}

public class WhatsAppMessageKey
{
    [JsonPropertyName("remoteJid")]
    public string RemoteJid { get; set; } = string.Empty;

    [JsonPropertyName("fromMe")]
    public bool FromMe { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class WhatsAppMessageBody
{
    [JsonPropertyName("conversation")]
    public string Conversation { get; set; } = string.Empty;

    [JsonPropertyName("extendedTextMessage")]
    public WhatsAppExtendedTextMessage? ExtendedTextMessage { get; set; }
}

public class WhatsAppExtendedTextMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
