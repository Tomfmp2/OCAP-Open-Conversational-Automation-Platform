namespace OCAP.Intelligence.Abstractions;

// Respuesta estandarizada producida por un proveedor de Inteligencia Artificial.
public class AiResponse
{
    public string GeneratedText { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
