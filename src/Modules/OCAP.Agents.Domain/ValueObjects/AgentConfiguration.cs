namespace OCAP.Agents.Domain.ValueObjects;

// Objeto de valor que encapsula la configuración e instrucciones operativas de un agente.
public class AgentConfiguration
{
    // Instrucción o prompt de sistema que guía el comportamiento del agente.
    public string SystemPrompt { get; private set; }

    // Parámetros de configuración adicionales (ej. temperatura, max_tokens, idioma).
    public IReadOnlyDictionary<string, string> Parameters { get; private set; }

    /// <summary>Parámetro con las colecciones de conocimiento que consulta el agente (ids separados por coma); sin él, todas.</summary>
    public const string KnowledgeBaseIdsParameter = "KnowledgeBaseIds";

    public IReadOnlyList<Guid> KnowledgeBaseIds =>
        Parameters.TryGetValue(KnowledgeBaseIdsParameter, out var raw)
            ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => Guid.TryParse(v, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToList()
            : [];

    /// <summary>Parámetro "false" si el agente no debe recordar a los contactos entre conversaciones.</summary>
    public const string RememberContactsParameter = "RememberContacts";

    public bool RemembersContacts => !Parameters.TryGetValue(RememberContactsParameter, out var value) || value != "false";

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
