using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.Ollama;

// Adaptador del proveedor Ollama (Llama 3 / Mistral self-hosted) que implementa IAiProvider.
public class OllamaAiProvider : IAiProvider
{
    private readonly AiProviderSettings _settings;

    public OllamaAiProvider(AiProviderSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Estructura base preparada para la integración con Ollama Local Server.
        var response = new AiResponse
        {
            GeneratedText = $"[Ollama Self-Hosted - {request.UserMessage}] Respuesta generada localmente.",
            TokensUsed = 95,
            ModelName = _settings.ModelName ?? "llama3",
            ProviderName = "OllamaLocal"
        };

        return Task.FromResult(response);
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Intent("GeneralQuery", 0.82f));
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<T?>(default);
    }

    public AiModelInformation GetModelInformation()
    {
        return new AiModelInformation
        {
            Provider = "OllamaLocal",
            Model = _settings.ModelName ?? "llama3",
            ContextSize = 8192,
            Capabilities = new List<string> { "text-generation", "local-privacy", "self-hosted" }
        };
    }
}
