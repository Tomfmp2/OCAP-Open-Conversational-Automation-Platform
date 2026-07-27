using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Abstractions.Ports;

// Puerto de persistencia para el contexto conversacional temporal.
public interface IConversationContextRepository
{
    // Obtiene el contexto conversacional asociado a un ID de conversación.
    Task<ConversationContext?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

    // Guarda o actualiza el contexto conversacional.
    Task SaveAsync(ConversationContext context, CancellationToken cancellationToken = default);
}
