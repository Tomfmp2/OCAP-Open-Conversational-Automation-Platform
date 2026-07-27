namespace OCAP.Channels.Abstractions.Models;

// Información descriptiva y estado de un proveedor de canal registrado en OCAP.
public class ChannelMetadata
{
    // Identificador único interno del canal (ej. "channel-mock-01", "channel-whatsapp-main").
    public string ChannelId { get; set; } = string.Empty;

    // Nombre visible del canal (ej. "WhatsApp", "Telegram", "Mock Channel").
    public string ChannelName { get; set; } = string.Empty;

    // Versión de la implementación del adaptador del canal.
    public string Version { get; set; } = "1.0.0";

    // Indica si el canal se encuentra activo y habilitado para procesar tráfico.
    public bool IsEnabled { get; set; } = true;
}
