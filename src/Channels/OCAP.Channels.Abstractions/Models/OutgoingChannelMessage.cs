namespace OCAP.Channels.Abstractions.Models;

// Modelo que representa un mensaje saliente enviado hacia un canal de comunicación externo.
// Desacopla la lógica de envío de respuestas de los detalles técnicos del proveedor final.
public class OutgoingChannelMessage
{
    // Identificador único del usuario destino en el canal externo (ej. número de teléfono o ID de chat).
    public string DestinationUserId { get; set; } = string.Empty;

    // Contenido textual de la respuesta a transmitir.
    public string Message { get; set; } = string.Empty;

    // Nombre del canal de destino (ej. "WhatsApp", "Telegram", "Mock").
    public string ChannelName { get; set; } = string.Empty;

    // Fecha y hora en que se despachó el mensaje hacia el canal externo.
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Metadatos opcionales para personalizar el despacho en el proveedor (ej. plantillas, botones, adjuntos).
    public Dictionary<string, string> Metadata { get; set; } = new();
}
