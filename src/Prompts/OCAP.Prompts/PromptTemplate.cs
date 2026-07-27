namespace OCAP.Prompts;

// Plantilla de prompt estructurada que soporta variables dinámicas y construcción de instrucciones.
public class PromptTemplate
{
    public string Name { get; set; } = "DefaultTemplate";
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, string> DynamicVariables { get; set; } = new();
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;

    // Renderiza el System Prompt reemplazando marcadores de posición dinámicos.
    public string RenderSystemPrompt()
    {
        var result = SystemPrompt;
        foreach (var (key, value) in DynamicVariables)
        {
            result = result.Replace($"{{{key}}}", value ?? string.Empty);
        }
        return result;
    }

    // Renderiza el User Prompt reemplazando marcadores de posición dinámicos.
    public string RenderUserPrompt()
    {
        var result = UserPrompt;
        foreach (var (key, value) in DynamicVariables)
        {
            result = result.Replace($"{{{key}}}", value ?? string.Empty);
        }
        return result;
    }
}
