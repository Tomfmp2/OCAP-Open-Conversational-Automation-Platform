using System.Text.Json.Serialization;

namespace OCAP.Channels.WhatsApp.Evolution;

// Respuesta tipada retornada por las llamadas HTTP a Evolution API.
public class EvolutionApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public EvolutionMessageKey? Key { get; set; }
}

// Clave identificadora del mensaje retornada por Evolution API.
public class EvolutionMessageKey
{
    [JsonPropertyName("remoteJid")]
    public string RemoteJid { get; set; } = string.Empty;

    [JsonPropertyName("fromMe")]
    public bool FromMe { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
