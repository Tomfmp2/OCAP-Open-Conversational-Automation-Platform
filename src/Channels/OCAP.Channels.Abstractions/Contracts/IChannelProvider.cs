using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Abstractions.Contracts;

// Contrato principal que debe implementar todo canal intercambiable en OCAP.
// Define el ciclo de vida del proveedor del canal y expone sus componentes de recepción y envío.
public interface IChannelProvider
{
    // Obtiene los metadatos descriptivos y el estado actual del canal.
    ChannelMetadata Metadata { get; }

    // Componente encargado del envío de mensajes a través de este canal.
    IMessageSender Sender { get; }

    // Componente encargado de la recepción de mensajes desde este canal.
    IMessageReceiver Receiver { get; }

    // Inicializa la conexión y recursos del canal (ej. webhooks, polling, autenticación).
    Task InitializeAsync(CancellationToken cancellationToken = default);

    // Detiene de forma segura la ejecución del canal y libera recursos asignados.
    Task StopAsync(CancellationToken cancellationToken = default);
}
