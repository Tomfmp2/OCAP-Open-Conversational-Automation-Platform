using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Abstractions.Contracts;

// Contrato genérico para emisores de mensajes hacia canales externos.
// Este puerto permite transmitir respuestas hacia cualquier plataforma sin acoplamiento a su SDK.
public interface IMessageSender
{
    // Transmite un mensaje de respuesta hacia un destinatario en la plataforma externa.
    // Retorna true si la plataforma externa confirmó la recepción del mensaje; de lo contrario, false.
    Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default);
}
