namespace OCAP.Agents.Domain.ValueObjects;

// Objeto de valor que encapsula la configuración e instrucciones operativas de un agente.
public class AgentConfiguration
{
    // Instrucción o prompt de sistema que guía el comportamiento del agente.
    public string SystemPrompt { get; private set; }

    // Parámetros de configuración adicionales (ej. temperatura, max_tokens, idioma).
    public IReadOnlyDictionary<string, string> Parameters { get; private set; }

    // Lista de herramientas o capacidades habilitadas para este agente.
    public IReadOnlyCollection<string> AllowedToolNames { get; private set; }

    private AgentConfiguration()
    {
        SystemPrompt = string.Empty;
        Parameters = new Dictionary<string, string>();
        AllowedToolNames = new List<string>();
    }

    public AgentConfiguration(
        string systemPrompt,
        Dictionary<string, string>? parameters = null,
        List<string>? allowedToolNames = null)
    {
        SystemPrompt = systemPrompt?.Trim() ?? "Eres un asistente virtual de OCAP útil y respetuoso.";
        Parameters = parameters ?? new Dictionary<string, string>();
        AllowedToolNames = allowedToolNames ?? new List<string>();
    }
}
