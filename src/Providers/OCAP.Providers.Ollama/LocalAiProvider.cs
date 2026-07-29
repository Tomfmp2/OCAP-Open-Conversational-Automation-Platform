using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.Ollama;

// Proveedor para ejecución de modelos locales (Local Models / LLaMA / ONNX / Custom Local HTTP Endpoint).
public class LocalAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings _settings;

    public string Name => "Local";

    public LocalAiProvider(HttpClient httpClient, AiProviderSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var stopwatch = Stopwatch.StartNew();

        var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl) ? "http://localhost:8080" : _settings.BaseUrl;
        
        var payload = new
        {
            prompt = request.UserMessage,
            system = request.SystemInstructions,
            temperature = request.Temperature ?? _settings.Temperature,
            max_tokens = request.MaxTokens ?? _settings.MaxTokens
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var responseMessage = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/v1/completions", jsonContent, cancellationToken);

        responseMessage.EnsureSuccessStatusCode();

        var body = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
            ? choices[0].GetProperty("text").GetString() ?? string.Empty
            : body;

        stopwatch.Stop();
        return new AiResponse
        {
            GeneratedText = text,
            TokensUsed = 0,
            ModelName = _settings.ModelName ?? "local-model",
            ProviderName = Name,
            Metadata = new Dictionary<string, object> { ["LatencyMs"] = stopwatch.Elapsed.TotalMilliseconds }
        };
    }

    public IAsyncEnumerable<string> StreamResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming is not currently supported by LocalAiProvider.");
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Intent analysis is not natively supported by LocalAiProvider completions API.");
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(text));
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public AiModelInformation GetModelInformation()
    {
        return new AiModelInformation
        {
            Provider = Name,
            Model = _settings.ModelName ?? "local-llama3",
            ContextSize = 8192,
            Capabilities = new List<string> { "chat", "offline-execution", "local-privacy" }
        };
    }

    public async Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl) ? "http://localhost:8080" : _settings.BaseUrl;
        bool isAlive = false;

        try
        {
            using var response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/health", cancellationToken);
            isAlive = response.IsSuccessStatusCode;
        }
        catch
        {
            isAlive = false;
        }

        stopwatch.Stop();
        return new ProviderHealth
        {
            ProviderName = Name,
            IsHealthy = isAlive,
            LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
            ModelList = new List<string> { _settings.ModelName ?? "local-model" },
            Version = "v1.0.0",
            StatusMessage = isAlive ? "Local Server Connected" : "Local Server Unreachable"
        };
    }

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> models = new List<string> { "local-llama3", "local-phi3", "local-mistral", "local-custom" };
        return Task.FromResult(models);
    }
}
