using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Abstractions;

// Contrato general agnóstico que debe implementar cualquier proveedor de IA Generativa.
public interface IAiProvider
{
    // Nombre único del proveedor (ej. "OpenAI", "Gemini", "Ollama", "Mock").
    string Name { get; }

    // Genera una respuesta conversacional basada en la solicitud y contexto.
    Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default);

    // Genera una respuesta en flujo continuo (Streaming) carácter por carácter / token por token.
    IAsyncEnumerable<string> StreamResponseAsync(AiRequest request, CancellationToken cancellationToken = default);

    // Analiza la intención conversacional del mensaje del usuario.
    Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default);

    // Extrae información estructurada serializada a partir de un texto plano.
    Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default);

    // Obtiene la información y capacidades del modelo de IA configurado.
    AiModelInformation GetModelInformation();

    // Evalúa la salud, disponibilidad y latencia actual del proveedor.
    Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default);

    // Obtiene la lista de modelos disponibles en la API del proveedor.
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
