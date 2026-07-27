namespace OCAP.Core.Entities;

// Entidad de persistencia que audita el historial de ejecuciones de herramientas en el sistema.
public class ToolExecution
{
    public Guid Id { get; private set; }
    public Guid AgentId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ConversationId { get; private set; }
    public string ToolName { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    private ToolExecution() { } // Constructor ORM

    public ToolExecution(Guid id, Guid agentId, Guid userId, Guid conversationId, string toolName, bool success, string? errorCode = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de ejecución no puede ser vacío.", nameof(id));

        Id = id;
        AgentId = agentId;
        UserId = userId;
        ConversationId = conversationId;
        ToolName = toolName ?? string.Empty;
        Success = success;
        ErrorCode = errorCode;
        ExecutedAt = DateTime.UtcNow;
    }
}
