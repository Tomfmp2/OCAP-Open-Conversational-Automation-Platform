namespace OCAP.Intelligence.Application.Services;

// Servicio de razonamiento que coordina contexto, IA Generativa, herramientas y permisos.
public interface IAgentReasoningService
{
    // Procesa el mensaje a través del motor de IA Generativa y ejecuta acciones si corresponde.
    Task<string> ProcessReasoningAsync(Guid agentId, Guid userId, Guid conversationId, string userMessage, CancellationToken cancellationToken = default);
}
