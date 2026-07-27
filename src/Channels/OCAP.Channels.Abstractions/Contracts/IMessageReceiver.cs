using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Abstractions.Contracts;

// Contrato genérico para receptores de mensajes desde canales externos.
// Este puerto permite recibir mensajes de cualquier proveedor sin modificar los casos de uso.
public interface IMessageReceiver
{
    // Procesa un mensaje entrante proveniente de una plataforma externa.
    // Retorna true si el mensaje fue aceptado y procesado exitosamente; de lo contrario, false.
    Task<bool> ReceiveMessageAsync(IncomingChannelMessage message, CancellationToken cancellationToken = default);
}
