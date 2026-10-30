using System.Text.Json.Serialization;

namespace OCAP.Channels.Telegram.DTOs;

public class TelegramSendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    // Sin formato no se envía el campo: Telegram rechaza parse_mode vacío o nulo con algunos clientes HTTP.
    [JsonPropertyName("parse_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParseMode { get; set; }
}
