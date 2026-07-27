namespace OCAP.Api.Models.Dashboard;

// DTO para el registro histórico de ejecuciones de herramientas.
public class ExecutionDto
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid ConversationId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ExecutedAt { get; set; }
}
