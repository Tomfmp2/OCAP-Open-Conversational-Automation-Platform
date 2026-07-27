using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.OpenAI;

// Adaptador del proveedor OpenAI (GPT-4o / GPT-3.5) que implementa IAiProvider.
public class OpenAiProvider : IAiProvider
{
    private readonly AiProviderSettings _settings;

    public OpenAiProvider(AiProviderSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Estructura base preparada para la integración con la API de OpenAI en versiones futuras.
        var response = new AiResponse
        {
            GeneratedText = $"[OpenAI Provider - {request.UserMessage}] Respuesta estructurada lista para conexión real.",
            TokensUsed = 120,
            ModelName = _settings.ModelName ?? "gpt-4o",
            ProviderName = "OpenAI"
        };

        return Task.FromResult(response);
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Intent("GeneralQuery", 0.85f));
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<T?>(default);
    }

    public AiModelInformation GetModelInformation()
    {
        return new AiModelInformation
        {
            Provider = "OpenAI",
            Model = _settings.ModelName ?? "gpt-4o",
            ContextSize = 128000,
            Capabilities = new List<string> { "text-generation", "function-calling", "vision", "json-schema" }
        };
    }
}
