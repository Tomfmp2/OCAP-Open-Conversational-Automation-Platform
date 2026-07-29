using System.Text.Json.Serialization;

namespace OCAP.Channels.WhatsApp.DTOs;

public class WhatsAppCloudWebhookPayload
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("entry")]
    public List<WhatsAppWebhookEntry> Entry { get; set; } = new();
}

public class WhatsAppWebhookEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("changes")]
    public List<WhatsAppWebhookChange> Changes { get; set; } = new();
}

public class WhatsAppWebhookChange
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public WhatsAppWebhookValue Value { get; set; } = new();
}

public class WhatsAppWebhookValue
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public WhatsAppWebhookMetadata? Metadata { get; set; }

    [JsonPropertyName("contacts")]
    public List<WhatsAppWebhookContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WhatsAppWebhookMessage>? Messages { get; set; }
    
    [JsonPropertyName("statuses")]
    public List<WhatsAppWebhookStatus>? Statuses { get; set; }
}

public class WhatsAppWebhookMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string DisplayPhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("phone_number_id")]
    public string PhoneNumberId { get; set; } = string.Empty;
}

public class WhatsAppWebhookContact
{
    [JsonPropertyName("profile")]
    public WhatsAppWebhookProfile? Profile { get; set; }

    [JsonPropertyName("wa_id")]
    public string WaId { get; set; } = string.Empty;
}

public class WhatsAppWebhookProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class WhatsAppWebhookMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public WhatsAppWebhookText? Text { get; set; }
}

public class WhatsAppWebhookText
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class WhatsAppWebhookStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;
}
