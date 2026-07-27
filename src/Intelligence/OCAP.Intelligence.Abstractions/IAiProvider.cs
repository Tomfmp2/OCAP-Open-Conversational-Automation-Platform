using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Abstractions;

// Contrato general agnóstico que debe implementar cualquier proveedor de IA Generativa.
public interface IAiProvider
{
    // Genera una respuesta conversacional basada en la solicitud y contexto.
    Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default);

    // Analiza la intención conversacional del mensaje del usuario.
    Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default);

    // Extrae información estructurada serializada a partir de un texto plano.
    Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default);

    // Obtiene la información y capacidades del modelo de IA configurado.
    AiModelInformation GetModelInformation();
}
