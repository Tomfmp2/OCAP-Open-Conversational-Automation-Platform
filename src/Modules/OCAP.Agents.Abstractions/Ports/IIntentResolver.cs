using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Abstractions.Ports;

// Puerto encubridor de resolución de intenciones a partir de mensajes y contexto conversacional.
// Permite intercambiar motores de clasificación de intenciones (reglas, expresiones regulares, IA local o LLMs).
public interface IIntentResolver
{
    // Analiza el mensaje recibido y resuelve la intencionalidad correspondiente.
    Task<Intent> ResolveIntentAsync(string message, ConversationContext? context, CancellationToken cancellationToken = default);
}
