namespace OCAP.Channels.Abstractions.Models;

// Modelo que representa un mensaje entrante recibido desde un canal de comunicación externo.
// Permite que la plataforma procese mensajes de cualquier proveedor sin conocer detalles específicos de su API.
public class IncomingChannelMessage
{
    // Identificador único del usuario en el canal externo (ej. número de teléfono en WhatsApp, username en Telegram).
    public string ExternalUserId { get; set; } = string.Empty;

    // Contenido textual del mensaje recibido.
    public string Message { get; set; } = string.Empty;

    // Nombre identificador del canal de origen (ej. "WhatsApp", "Telegram", "Mock").
    public string ChannelName { get; set; } = string.Empty;

    // Fecha y hora en que se recibió el mensaje en el adaptador del canal.
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    // Metadatos adicionales opcionales específicos del proveedor (ej. ID de mensaje original, headers, tokens).
    public Dictionary<string, string> Metadata { get; set; } = new();
}
