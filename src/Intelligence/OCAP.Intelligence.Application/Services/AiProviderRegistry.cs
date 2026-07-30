using System.Collections.Concurrent;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Application.Services;

// Registro runtime y fábrica de proveedores de IA compatibles con OCAP.
public class AiProviderRegistry : IAiProviderRegistry
{
    private readonly ConcurrentDictionary<string, IAiProvider> _registeredProviders = new(StringComparer.OrdinalIgnoreCase);
    private readonly HttpClient _httpClient;

    public AiProviderRegistry(IEnumerable<IAiProvider> initialProviders, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();

        foreach (var provider in initialProviders)
        {
            _registeredProviders[provider.Name] = provider;
        }
    }

    public void RegisterProvider(IAiProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        _registeredProviders[provider.Name] = provider;
    }

    public IAiProvider? GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName)) return null;
        return _registeredProviders.TryGetValue(providerName.Trim(), out var provider) ? provider : null;
    }

    public IReadOnlyList<string> GetRegisteredProviderNames()
    {
        return _registeredProviders.Keys.ToList();
    }

    public IAiProvider CreateDynamicProvider(string providerName, string modelName, string apiKey, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentException("ProviderName is required.", nameof(providerName));

        var settings = new AiProviderSettings
        {
            ApiKey = apiKey,
            ModelName = modelName,
            BaseUrl = baseUrl ?? string.Empty
        };

        var normName = providerName.Trim().ToLowerInvariant();

        return normName switch
        {
            "openai" => new OCAP.Providers.OpenAI.OpenAiProvider(_httpClient, settings),
            "gemini" => new OCAP.Providers.Gemini.GeminiAiProvider(_httpClient, settings),
            "ollama" => new OCAP.Providers.Ollama.OllamaAiProvider(_httpClient, settings),
            "local" => new OCAP.Providers.Ollama.LocalAiProvider(_httpClient, settings),
            "claude" or "anthropic" => new OCAP.Providers.Claude.ClaudeAiProvider(_httpClient, settings),
            _ => GetProvider(providerName) ?? throw new NotSupportedException($"Proveedor de IA no soportado o no configurado: {providerName}")
        };
    }
}
