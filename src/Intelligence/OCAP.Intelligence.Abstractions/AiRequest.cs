namespace OCAP.Intelligence.Abstractions;

// Modelo de solicitud estandarizado para consultar un proveedor de Inteligencia Artificial.
public class AiRequest
{
    public Guid AgentId { get; set; }
    public Guid ConversationId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public List<string> ConversationHistory { get; set; } = new();
    public string SystemInstructions { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}
