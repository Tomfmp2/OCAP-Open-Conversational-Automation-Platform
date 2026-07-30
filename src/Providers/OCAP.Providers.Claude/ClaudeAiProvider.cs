using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Providers.Claude;

public sealed class ClaudeAiProvider : IAiProvider
{
    private const string DefaultBaseUrl = "https://api.anthropic.com/v1";
    private const string DefaultModel = "claude-3-5-sonnet-latest";
    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings _settings;

    public ClaudeAiProvider(HttpClient httpClient, AiProviderSettings settings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public string Name => "Claude";

    public async Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureConfigured();
        var stopwatch = Stopwatch.StartNew();

        using var response = await SendMessagesAsync(request, stream: false, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, json);
        stopwatch.Stop();

        using var document = JsonDocument.Parse(json);
        var text = string.Join(
            string.Empty,
            document.RootElement.GetProperty("content")
                .EnumerateArray()
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "text")
                .Select(item => item.GetProperty("text").GetString() ?? string.Empty));

        var tokens = 0;
        if (document.RootElement.TryGetProperty("usage", out var usage))
        {
            tokens += usage.TryGetProperty("input_tokens", out var input) ? input.GetInt32() : 0;
            tokens += usage.TryGetProperty("output_tokens", out var output) ? output.GetInt32() : 0;
        }

        return new AiResponse
        {
            GeneratedText = text,
            TokensUsed = tokens,
            ModelName = Model,
            ProviderName = Name,
            Metadata = new Dictionary<string, object> { ["LatencyMs"] = stopwatch.Elapsed.TotalMilliseconds }
        };
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureConfigured();

        using var response = await SendMessagesAsync(request, stream: true, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, error);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data.Length == 0 || data == "[DONE]")
            {
                continue;
            }

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            if (root.TryGetProperty("type", out var eventType)
                && eventType.GetString() == "content_block_delta"
                && root.TryGetProperty("delta", out var delta)
                && delta.TryGetProperty("text", out var text))
            {
                yield return text.GetString() ?? string.Empty;
            }
        }
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = (message ?? string.Empty).ToLowerInvariant();
        if (text.Contains("reunión") || text.Contains("cita")) return Task.FromResult(new Intent("CreateReminder", 0.95f));
        if (text.Contains("correo") || text.Contains("email")) return Task.FromResult(new Intent("SendEmail", 0.93f));
        return Task.FromResult(new Intent("GeneralQuery", 0.88f));
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(text));
        }
        catch (JsonException)
        {
            return Task.FromResult<T?>(default);
        }
    }

    public AiModelInformation GetModelInformation() => new()
    {
        Provider = Name,
        Model = Model,
        ContextSize = 200000,
        Capabilities = new List<string> { "chat", "streaming", "json-response", "function-calling", "vision" }
    };

    public async Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var models = await GetAvailableModelsAsync(cancellationToken);
            return new ProviderHealth
            {
                ProviderName = Name,
                IsHealthy = true,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                ModelList = models.ToList(),
                Version = "2023-06-01",
                StatusMessage = "Anthropic API conectada"
            };
        }
        catch (Exception exception)
        {
            return new ProviderHealth
            {
                ProviderName = Name,
                IsHealthy = false,
                LatencyMs = stopwatch.Elapsed.TotalMilliseconds,
                ModelList = new List<string> { DefaultModel },
                Version = "2023-06-01",
                StatusMessage = exception.Message
            };
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/models");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, json);

        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToArray();
    }

    private async Task<HttpResponseMessage> SendMessagesAsync(
        AiRequest request,
        bool stream,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = Model,
            ["max_tokens"] = request.MaxTokens ?? _settings.MaxTokens,
            ["temperature"] = request.Temperature ?? _settings.Temperature,
            ["messages"] = request.ConversationHistory
                .Select(history => new { role = "user", content = history })
                .Append(new { role = "user", content = request.UserMessage })
                .ToArray(),
            ["stream"] = stream
        };

        if (!string.IsNullOrWhiteSpace(request.SystemInstructions))
        {
            payload["system"] = request.SystemInstructions;
        }

        using var httpRequest = CreateRequest(HttpMethod.Post, $"{BaseUrl}/messages");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await _httpClient.SendAsync(
            httpRequest,
            stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.TryAddWithoutValidation("x-api-key", _settings.ApiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        return request;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "mock-key")
        {
            throw new InvalidOperationException("No se ha configurado una API Key válida para Claude.");
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Error en Anthropic API ({response.StatusCode}): {body}");
        }
    }

    private string BaseUrl => string.IsNullOrWhiteSpace(_settings.BaseUrl)
        ? DefaultBaseUrl
        : _settings.BaseUrl.TrimEnd('/');

    private string Model => string.IsNullOrWhiteSpace(_settings.ModelName) ? DefaultModel : _settings.ModelName;
}
