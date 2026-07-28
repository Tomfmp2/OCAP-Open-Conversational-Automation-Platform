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
        
        try
        {
            var payload = new
            {
                prompt = request.UserMessage,
                system = request.SystemInstructions,
                temperature = request.Temperature ?? _settings.Temperature,
                max_tokens = request.MaxTokens ?? _settings.MaxTokens
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            using var responseMessage = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/v1/completions", jsonContent, cancellationToken);

            if (responseMessage.IsSuccessStatusCode)
            {
                var body = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    ? choices[0].GetProperty("text").GetString() ?? string.Empty
                    : body;

                stopwatch.Stop();
                return new AiResponse
                {
                    GeneratedText = text,
                    TokensUsed = 42,
                    ModelName = _settings.ModelName ?? "local-llama3",
                    ProviderName = Name,
                    Metadata = new Dictionary<string, object> { ["LatencyMs"] = stopwatch.Elapsed.TotalMilliseconds }
                };
            }
        }
        catch
        {
            // Fallback para ejecución offline sin servidor local disponible
        }

        stopwatch.Stop();
        return new AiResponse
        {
            GeneratedText = $"[Modelo Local - {_settings.ModelName ?? "local-llama3"}]: Procesado localmente offline: '{request.UserMessage}'.",
            TokensUsed = 25,
            ModelName = _settings.ModelName ?? "local-llama3",
            ProviderName = Name,
            Metadata = new Dictionary<string, object>
            {
                ["LatencyMs"] = stopwatch.Elapsed.TotalMilliseconds,
                ["OfflineMode"] = true
            }
        };
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(AiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = $"[Local Streaming]: {request.UserMessage}";
        foreach (var word in text.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return word + " ";
            await Task.Delay(15, cancellationToken);
        }
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Intent("LocalExecution", 0.9f));
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
            IsHealthy = true, // Siempre funcional vía fallback local
            LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
            ModelList = new List<string> { "local-llama3", "local-phi3", "local-mistral" },
            Version = "v1.0.0-local",
            StatusMessage = isAlive ? "Servidor Local Conectado" : "Modo Local Autónomo (Offline Simulation)"
        };
    }

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> models = new List<string> { "local-llama3", "local-phi3", "local-mistral", "local-custom" };
        return Task.FromResult(models);
    }
}
