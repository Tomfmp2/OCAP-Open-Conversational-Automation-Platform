using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.Abstractions.Contracts;

/// <summary>
/// Genera la respuesta del asistente a un mensaje entrante de un canal externo: registra al contacto y el mensaje en
/// su conversación y devuelve el texto que el canal debe enviar. Así los adaptadores de canal no dependen de la API.
/// </summary>
public interface IConversationResponder
{
    /// <param name="tenantId">Organización dueña del canal, ya verificada por quien llama.</param>
    /// <param name="channelConnectionId">Conexión que recibió el mensaje: define qué agente responde.</param>
    /// <param name="message">Mensaje con <c>ChannelName</c> y <c>ExternalUserId</c> del contacto.</param>
    /// <param name="displayName">Nombre del contacto para su ficha, si el canal lo conoce.</param>
    /// <returns>Texto a enviar, o null si una persona del equipo atiende la conversación y no hay que responder.</returns>
    Task<string?> RespondAsync(
        Guid tenantId,
        Guid? channelConnectionId,
        IncomingChannelMessage message,
        string displayName,
        CancellationToken cancellationToken);
}
