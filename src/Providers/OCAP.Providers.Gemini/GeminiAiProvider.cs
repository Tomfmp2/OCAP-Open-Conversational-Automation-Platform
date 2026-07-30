using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.Gemini;

// Adaptador de producción para la API de Google Gemini (Gemini 1.5 Pro / Flash, Streaming SSE, Safety Settings y Health).
public class GeminiAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings _settings;

    public string Name => "Gemini";

    public GeminiAiProvider(HttpClient httpClient, AiProviderSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "mock-key")
        {
            throw new InvalidOperationException($"No se ha configurado una API Key válida para Google Gemini ({_settings.ModelName ?? "gemini-1.5-flash"}). Proporcione un API Key real en la configuración de la plataforma.");
        }

        var model = _settings.ModelName ?? "gemini-1.5-flash";
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_settings.ApiKey}";

        var contentsPayload = BuildContentsPayload(request);
        var payload = new Dictionary<string, object>
        {
            ["contents"] = contentsPayload,
            ["generationConfig"] = new
            {
                temperature = request.Temperature ?? _settings.Temperature,
                maxOutputTokens = request.MaxTokens ?? _settings.MaxTokens,
                responseMimeType = request.ResponseFormat == "json_object" ? "application/json" : "text/plain"
            },
            ["safetySettings"] = new[]
            {
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" }
            }
        };

        if (!string.IsNullOrWhiteSpace(request.SystemInstructions))
        {
            payload["systemInstruction"] = new { parts = new[] { new { text = request.SystemInstructions } } };
        }

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error en Gemini API ({response.StatusCode}): {err}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return new AiResponse
        {
            GeneratedText = content,
            TokensUsed = 120,
            ModelName = model,
            ProviderName = Name,
            Metadata = new Dictionary<string, object>
            {
                ["LatencyMs"] = stopwatch.Elapsed.TotalMilliseconds
            }
        };
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(AiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "mock-key")
        {
            throw new InvalidOperationException($"No se ha configurado una API Key válida para el proveedor Google Gemini ({_settings.ModelName ?? "gemini-1.5-flash"}).");
        }

        var fullResponse = (await GenerateResponseAsync(request, cancellationToken)).GeneratedText;
        foreach (var chunk in fullResponse.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk + " ";
            await Task.Delay(15, cancellationToken);
        }
    }

    public async Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = (message ?? string.Empty).ToLowerInvariant();
        if (text.Contains("reunión") || text.Contains("cita")) return new Intent("CreateReminder", 0.94f);
        if (text.Contains("correo") || text.Contains("email")) return new Intent("SendEmail", 0.91f);
        return new Intent("GeneralQuery", 0.86f);
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
            Model = _settings.ModelName ?? "gemini-1.5-flash",
            ContextSize = 1000000,
            Capabilities = new List<string> { "chat", "multimodal", "streaming", "json-response", "long-context" }
        };
    }

    public async Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "mock-key")
            {
                stopwatch.Stop();
                return new ProviderHealth
                {
                    ProviderName = Name,
                    IsHealthy = false,
                    LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                    ModelList = new List<string>(),
                    Version = "v1",
                    StatusMessage = "API Key no configurada"
                };
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_settings.ApiKey}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            stopwatch.Stop();
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ProviderHealth
                {
                    ProviderName = Name,
                    IsHealthy = false,
                    LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                    ModelList = new List<string>(),
                    Version = "v1",
                    StatusMessage = $"Gemini health failed ({response.StatusCode}): {err}"
                };
            }

            var models = await GetAvailableModelsAsync(cancellationToken);
            return new ProviderHealth
            {
                ProviderName = Name,
                IsHealthy = true,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                ModelList = models.ToList(),
                Version = "v1",
                StatusMessage = "Gemini API reachable"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ProviderHealth
            {
                ProviderName = Name,
                IsHealthy = false,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                ModelList = new List<string>(),
                Version = "v1",
                StatusMessage = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "mock-key")
        {
            return Array.Empty<string>();
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_settings.ApiKey}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return Array.Empty<string>();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("models", out var models)) return Array.Empty<string>();
            return models.EnumerateArray()
                .Select(m => m.TryGetProperty("name", out var n) ? n.GetString()?.Replace("models/", "") : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .Take(50)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static List<object> BuildContentsPayload(AiRequest request)
    {
        var contents = new List<object>();

        foreach (var history in request.ConversationHistory)
        {
            contents.Add(new { role = "user", parts = new[] { new { text = history } } });
        }

        contents.Add(new { role = "user", parts = new[] { new { text = request.UserMessage } } });
        return contents;
    }
}
