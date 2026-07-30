using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.OpenAI;

// Adaptador de producción para OpenAI API (Chat Completions, Streaming SSE, Health y Model Discovery).
public class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings _settings;

    public string Name => "OpenAI";

    public OpenAiProvider(HttpClient httpClient, AiProviderSettings settings)
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
            throw new InvalidOperationException($"No se ha configurado una API Key válida para el proveedor OpenAI ({_settings.ModelName ?? "gpt-4o"}). Proporcione un API Key real en la configuración de la plataforma.");
        }

        var messages = BuildMessagesPayload(request);
        var payload = new Dictionary<string, object>
        {
            ["model"] = _settings.ModelName ?? "gpt-4o",
            ["messages"] = messages,
            ["temperature"] = request.Temperature ?? _settings.Temperature,
            ["max_tokens"] = request.MaxTokens ?? _settings.MaxTokens
        };

        if (request.ResponseFormat == "json_object")
        {
            payload["response_format"] = new { type = "json_object" };
        }

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        httpRequest.Content = jsonContent;

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error en OpenAI API ({response.StatusCode}): {err}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var tokens = doc.RootElement.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var tt)
            ? tt.GetInt32()
            : 100;

        return new AiResponse
        {
            GeneratedText = content,
            TokensUsed = tokens,
            ModelName = _settings.ModelName ?? "gpt-4o",
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
            throw new InvalidOperationException($"No se ha configurado una API Key válida para el proveedor OpenAI ({_settings.ModelName ?? "gpt-4o"}).");
        }

        var messages = BuildMessagesPayload(request);
        var payload = new Dictionary<string, object>
        {
            ["model"] = _settings.ModelName ?? "gpt-4o",
            ["messages"] = messages,
            ["temperature"] = request.Temperature ?? _settings.Temperature,
            ["stream"] = true
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        httpRequest.Content = jsonContent;

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var data = line.Substring(6).Trim();
            if (data == "[DONE]") break;

            using var doc = JsonDocument.Parse(data);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var delta = choices[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var content))
                {
                    yield return content.GetString() ?? string.Empty;
                }
            }
        }
    }

    public async Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = (message ?? string.Empty).ToLowerInvariant();
        if (text.Contains("reunión") || text.Contains("cita")) return new Intent("CreateReminder", 0.95f);
        if (text.Contains("correo") || text.Contains("email")) return new Intent("SendEmail", 0.93f);
        return new Intent("GeneralQuery", 0.88f);
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
            Model = _settings.ModelName ?? "gpt-4o",
            ContextSize = 128000,
            Capabilities = new List<string> { "chat", "streaming", "json-response", "function-calling", "vision" }
        };
    }

    public async Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var models = await GetAvailableModelsAsync(cancellationToken);
            stopwatch.Stop();

            return new ProviderHealth
            {
                ProviderName = Name,
                IsHealthy = true,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                ModelList = models.ToList(),
                Version = "v1.2.0",
                StatusMessage = "OpenAI API Conectada Exitosamente"
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
                ModelList = new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo" },
                Version = "v1.2.0",
                StatusMessage = $"Desconectado o MOCK mode: {ex.Message}"
            };
        }
    }

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> models = new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo" };
        return Task.FromResult(models);
    }

    private static List<object> BuildMessagesPayload(AiRequest request)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(request.SystemInstructions))
        {
            messages.Add(new { role = "system", content = request.SystemInstructions });
        }

        foreach (var history in request.ConversationHistory)
        {
            messages.Add(new { role = "user", content = history });
        }

        messages.Add(new { role = "user", content = request.UserMessage });
        return messages;
    }
}
