using System.Text.Json.Serialization;

namespace OCAP.Channels.Telegram.DTOs;

public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }

    [JsonPropertyName("edited_message")]
    public TelegramMessage? EditedMessage { get; set; }
}
