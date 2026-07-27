namespace OCAP.Intelligence.Domain;

// Representa la información y capacidades de un modelo de Inteligencia Artificial Generativa.
public class AiModelInformation
{
    // Nombre identificador del proveedor del modelo (ej. OpenAI, Gemini, Ollama).
    public string Provider { get; set; } = string.Empty;

    // Nombre específico del modelo (ej. gpt-4o, gemini-1.5-pro, llama3).
    public string Model { get; set; } = string.Empty;

    // Tamaño de la ventana de contexto soportada medida en tokens.
    public int ContextSize { get; set; } = 8192;

    // Capacidades soportadas por el modelo (ej. text-generation, function-calling, vision).
    public List<string> Capabilities { get; set; } = new();
}
