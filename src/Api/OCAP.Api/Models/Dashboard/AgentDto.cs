namespace OCAP.Api.Models.Dashboard;

// DTO descriptivo para la gestión de Agentes en la API del Dashboard.
public class AgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string SystemPrompt { get; set; } = string.Empty;
    public List<string> EnabledTools { get; set; } = new();
    /// <summary>Colecciones de conocimiento que consulta; vacía, todas.</summary>
    public List<Guid> KnowledgeBaseIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
