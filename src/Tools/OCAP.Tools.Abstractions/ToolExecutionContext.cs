namespace OCAP.Tools.Abstractions;

// Representa el contexto de ejecución inmutable para la invocación de una herramienta por un agente.
public class ToolExecutionContext
{
    // Identificador del agente ejecutor.
    public Guid AgentId { get; }

    // Identificador del usuario final solicitante.
    public Guid UserId { get; }

    // Identificador de la conversación activa.
    public Guid ConversationId { get; }

    // Identificador único de esta ejecución de herramienta.
    public Guid ExecutionId { get; }

    // Parámetros y argumentos suministrados para la ejecución.
    public IReadOnlyDictionary<string, object> Parameters { get; }

    // Estampa de tiempo UTC del momento de ejecución.
    public DateTime ExecutedAt { get; }

    public ToolExecutionContext(
        Guid agentId,
        Guid userId,
        Guid conversationId,
        Dictionary<string, object>? parameters = null,
        Guid? executionId = null)
    {
        AgentId = agentId;
        UserId = userId;
        ConversationId = conversationId;
        ExecutionId = executionId ?? Guid.NewGuid();
        Parameters = parameters ?? new Dictionary<string, object>();
        ExecutedAt = DateTime.UtcNow;
    }
}
