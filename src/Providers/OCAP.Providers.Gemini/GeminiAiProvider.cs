using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.Gemini;

/// <summary>
/// Adaptador Gemini. Prueba el modelo configurado y, si Google lo rechaza (404/new users),
/// cae a aliases actuales (3.5 / flash-latest).
/// </summary>
public class GeminiAiProvider : IAiProvider
{
    private static readonly string[] FallbackModels =
    {
        "gemini-3.5-flash",
        "gemini-flash-latest",
        "gemini-3-flash-preview",
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash-lite",
        "gemini-2.5-flash"
    };

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

        EnsureApiKeyConfigured();

        var stopwatch = Stopwatch.StartNew();
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

        var json = JsonSerializer.Serialize(payload);
        Exception? lastError = null;

        foreach (var model in ResolveModelCandidates())
        {
            var endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _settings.ApiKey.Trim());
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                using var doc = JsonDocument.Parse(body);
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

            lastError = new InvalidOperationException($"Error en Gemini API ({response.StatusCode}) modelo={model}: {body}");

            // Reintentar solo si el modelo no está disponible para esta key / época.
            var shouldFallback =
                response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                body.Contains("no longer available", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("is not found", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase);

            if (!shouldFallback)
            {
                stopwatch.Stop();
                throw lastError;
            }
        }

        stopwatch.Stop();
        throw lastError ?? new InvalidOperationException("Gemini no devolvió respuesta con ningún modelo candidato.");
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fullResponse = (await GenerateResponseAsync(request, cancellationToken)).GeneratedText;
        foreach (var chunk in fullResponse.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk + " ";
            await Task.Delay(15, cancellationToken);
        }
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = (message ?? string.Empty).ToLowerInvariant();
        if (text.Contains("reunión") || text.Contains("cita")) return Task.FromResult(new Intent("CreateReminder", 0.94f));
        if (text.Contains("correo") || text.Contains("email")) return Task.FromResult(new Intent("SendEmail", 0.91f));
        return Task.FromResult(new Intent("GeneralQuery", 0.86f));
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
            Model = ResolveModelCandidates().First(),
            ContextSize = 1000000,
            Capabilities = new List<string> { "chat", "multimodal", "streaming", "json-response", "long-context" }
        };
    }

    public async Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (!IsApiKeyConfigured())
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

            var url = "https://generativelanguage.googleapis.com/v1beta/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", _settings.ApiKey.Trim());
            using var response = await _httpClient.SendAsync(request, cancellationToken);
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
        if (!IsApiKeyConfigured())
        {
            return Array.Empty<string>();
        }

        try
        {
            var url = "https://generativelanguage.googleapis.com/v1beta/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", _settings.ApiKey.Trim());
            using var response = await _httpClient.SendAsync(request, cancellationToken);
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

    private void EnsureApiKeyConfigured()
    {
        if (!IsApiKeyConfigured())
        {
            throw new InvalidOperationException(
                "No se ha configurado una API Key válida para Google Gemini. Revisa AiProviders__Gemini__ApiKey en .env (installation.json no debe contener secretos).");
        }
    }

    private bool IsApiKeyConfigured()
    {
        var key = _settings.ApiKey?.Trim();
        return !string.IsNullOrWhiteSpace(key)
               && !string.Equals(key, "mock-key", StringComparison.OrdinalIgnoreCase)
               && key is not ("***" or "redacted" or "REDACTED");
    }

    private IEnumerable<string> ResolveModelCandidates()
    {
        var preferred = string.IsNullOrWhiteSpace(_settings.ModelName)
            ? "gemini-3.5-flash"
            : _settings.ModelName.Trim();

        // Nunca mandar 1.5 primero: keys nuevas lo rechazan (404) y confunde el diagnóstico.
        if (preferred.Contains("1.5", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(preferred, "gemini-2.0-flash", StringComparison.OrdinalIgnoreCase))
        {
            preferred = "gemini-3.5-flash";
        }

        yield return preferred;
        foreach (var m in FallbackModels)
        {
            if (!string.Equals(m, preferred, StringComparison.OrdinalIgnoreCase))
            {
                yield return m;
            }
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
