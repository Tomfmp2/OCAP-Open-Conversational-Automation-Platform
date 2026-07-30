using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Infrastructure.Embeddings;

internal static class EmbeddingVectorFactory
{
    public static EmbeddingVector FromChunk(KnowledgeChunk chunk, string provider, string model, float[] values)
        => new(
            chunk.Id,
            provider,
            model,
            values.Length,
            values,
            chunk.DocumentId,
            chunk.KnowledgeBaseId,
            chunk.TenantId,
            chunk.MetadataJson);

    public static EmbeddingVector FromText(Guid chunkId, string provider, string model, float[] values)
        => new(chunkId, provider, model, values.Length, values);
}

public sealed class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "OpenAI";

    private readonly HttpClient _httpClient;
    private readonly KnowledgeOptions _options;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;

    public OpenAiEmbeddingProvider(
        HttpClient httpClient,
        IOptions<KnowledgeOptions> options,
        ILogger<OpenAiEmbeddingProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(
        List<KnowledgeChunk> chunks,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        if (chunks.Count == 0) return [];

        var vectors = await EmbedTextsAsync(
            chunks.Select(c => c.Content).ToList(),
            model,
            cancellationToken);

        if (vectors.Count != chunks.Count)
            throw new InvalidOperationException($"OpenAI returned {vectors.Count} embeddings for {chunks.Count} chunks.");

        return chunks
            .Select((chunk, i) => EmbeddingVectorFactory.FromChunk(chunk, ProviderName, model, vectors[i]))
            .ToList();
    }

    public async Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(
        List<string> texts,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var vectors = await EmbedTextsAsync(texts, model, cancellationToken);
        return vectors
            .Select((v, i) => EmbeddingVectorFactory.FromText(Guid.NewGuid(), ProviderName, model, v))
            .ToList();
    }

    private async Task<List<float[]>> EmbedTextsAsync(List<string> texts, string model, CancellationToken cancellationToken)
    {
        var cfg = _options.OpenAI;
        var apiKey = ResolveApiKey(cfg.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "mock-key")
            throw new InvalidOperationException("OpenAI embedding API key is not configured (Knowledge:OpenAI:ApiKey or AiProviders:OpenAI:ApiKey).");

        var resolvedModel = string.IsNullOrWhiteSpace(model) ? cfg.DefaultModel : model;
        var baseUrl = string.IsNullOrWhiteSpace(cfg.BaseUrl) ? "https://api.openai.com/v1" : cfg.BaseUrl.TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model = resolvedModel,
            input = texts,
            encoding_format = "float"
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI embeddings failed ({Status}): {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"OpenAI embeddings request failed ({(int)response.StatusCode}): {body}");
        }

        var parsed = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(body, JsonOptions)
                     ?? throw new InvalidOperationException("OpenAI embeddings response was empty.");

        return parsed.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding ?? throw new InvalidOperationException("OpenAI returned a null embedding vector."))
            .ToList();
    }

    private string ResolveApiKey(string configured)
        => !string.IsNullOrWhiteSpace(configured)
            ? configured
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class OpenAiEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiEmbeddingData> Data { get; set; } = [];
    }

    private sealed class OpenAiEmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}

public sealed class GeminiEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "Gemini";

    private readonly HttpClient _httpClient;
    private readonly KnowledgeOptions _options;
    private readonly ILogger<GeminiEmbeddingProvider> _logger;

    public GeminiEmbeddingProvider(
        HttpClient httpClient,
        IOptions<KnowledgeOptions> options,
        ILogger<GeminiEmbeddingProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(
        List<KnowledgeChunk> chunks,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        if (chunks.Count == 0) return [];

        var vectors = await EmbedTextsAsync(chunks.Select(c => c.Content).ToList(), model, cancellationToken);
        if (vectors.Count != chunks.Count)
            throw new InvalidOperationException($"Gemini returned {vectors.Count} embeddings for {chunks.Count} chunks.");

        return chunks
            .Select((chunk, i) => EmbeddingVectorFactory.FromChunk(chunk, ProviderName, model, vectors[i]))
            .ToList();
    }

    public async Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(
        List<string> texts,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var vectors = await EmbedTextsAsync(texts, model, cancellationToken);
        return vectors
            .Select(v => EmbeddingVectorFactory.FromText(Guid.NewGuid(), ProviderName, model, v))
            .ToList();
    }

    private async Task<List<float[]>> EmbedTextsAsync(List<string> texts, string model, CancellationToken cancellationToken)
    {
        var cfg = _options.Gemini;
        var apiKey = ResolveApiKey(cfg.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "mock-key")
            throw new InvalidOperationException("Gemini embedding API key is not configured (Knowledge:Gemini:ApiKey or AiProviders:Gemini:ApiKey).");

        var resolvedModel = string.IsNullOrWhiteSpace(model) ? cfg.DefaultModel : model;
        var baseUrl = string.IsNullOrWhiteSpace(cfg.BaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : cfg.BaseUrl.TrimEnd('/');

        // Prefer batch endpoint when available; fall back to per-text embedContent.
        if (texts.Count > 1)
        {
            try
            {
                return await EmbedBatchAsync(baseUrl, resolvedModel, apiKey, texts, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini batchEmbedContents failed; falling back to sequential embedContent.");
            }
        }

        var result = new List<float[]>(texts.Count);
        foreach (var text in texts)
        {
            result.Add(await EmbedOneAsync(baseUrl, resolvedModel, apiKey, text, cancellationToken));
        }

        return result;
    }

    private async Task<List<float[]>> EmbedBatchAsync(
        string baseUrl,
        string model,
        string apiKey,
        List<string> texts,
        CancellationToken cancellationToken)
    {
        var url = $"{baseUrl}/models/{model}:batchEmbedContents?key={Uri.EscapeDataString(apiKey)}";
        var payload = new
        {
            requests = texts.Select(t => new
            {
                model = $"models/{model}",
                content = new { parts = new[] { new { text = t } } },
                outputDimensionality = _options.EmbeddingDimensions
            }).ToArray()
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini batch embeddings failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("embeddings", out var embeddings))
            throw new InvalidOperationException("Gemini batch response missing embeddings.");

        var list = new List<float[]>();
        foreach (var item in embeddings.EnumerateArray())
        {
            list.Add(ReadValues(item));
        }

        return list;
    }

    private async Task<float[]> EmbedOneAsync(
        string baseUrl,
        string model,
        string apiKey,
        string text,
        CancellationToken cancellationToken)
    {
        var url = $"{baseUrl}/models/{model}:embedContent?key={Uri.EscapeDataString(apiKey)}";
        var payload = new
        {
            content = new { parts = new[] { new { text } } },
            outputDimensionality = _options.EmbeddingDimensions
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini embeddings failed ({Status}): {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Gemini embeddings request failed ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("embedding", out var embedding))
            throw new InvalidOperationException("Gemini response missing embedding.");

        return ReadValues(embedding);
    }

    private static float[] ReadValues(JsonElement embeddingElement)
    {
        var valuesElement = embeddingElement.TryGetProperty("values", out var values)
            ? values
            : embeddingElement;

        return valuesElement.EnumerateArray().Select(v => v.GetSingle()).ToArray();
    }

    private string ResolveApiKey(string configured)
        => !string.IsNullOrWhiteSpace(configured)
            ? configured
            : Environment.GetEnvironmentVariable("GEMINI_API_KEY")
              ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
              ?? string.Empty;
}

public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "Ollama";

    private readonly HttpClient _httpClient;
    private readonly KnowledgeOptions _options;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(
        HttpClient httpClient,
        IOptions<KnowledgeOptions> options,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(
        List<KnowledgeChunk> chunks,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        if (chunks.Count == 0) return [];

        var vectors = await EmbedTextsAsync(chunks.Select(c => c.Content).ToList(), model, cancellationToken);
        if (vectors.Count != chunks.Count)
            throw new InvalidOperationException($"Ollama returned {vectors.Count} embeddings for {chunks.Count} chunks.");

        return chunks
            .Select((chunk, i) => EmbeddingVectorFactory.FromChunk(chunk, ProviderName, model, vectors[i]))
            .ToList();
    }

    public async Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(
        List<string> texts,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var vectors = await EmbedTextsAsync(texts, model, cancellationToken);
        return vectors
            .Select(v => EmbeddingVectorFactory.FromText(Guid.NewGuid(), ProviderName, model, v))
            .ToList();
    }

    private async Task<List<float[]>> EmbedTextsAsync(List<string> texts, string model, CancellationToken cancellationToken)
    {
        var cfg = _options.Ollama;
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? cfg.DefaultModel : model;
        var baseUrl = string.IsNullOrWhiteSpace(cfg.BaseUrl) ? "http://localhost:11434" : cfg.BaseUrl.TrimEnd('/');

        // Prefer /api/embed (batch) then fall back to /api/embeddings.
        try
        {
            return await EmbedBatchAsync(baseUrl, resolvedModel, texts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama /api/embed failed; falling back to /api/embeddings.");
        }

        var result = new List<float[]>(texts.Count);
        foreach (var text in texts)
        {
            result.Add(await EmbedOneAsync(baseUrl, resolvedModel, text, cancellationToken));
        }

        return result;
    }

    private async Task<List<float[]>> EmbedBatchAsync(
        string baseUrl,
        string model,
        List<string> texts,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/api/embed",
            new { model, input = texts },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama /api/embed failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("embeddings", out var embeddings))
            throw new InvalidOperationException("Ollama /api/embed response missing embeddings.");

        return embeddings.EnumerateArray()
            .Select(e => e.EnumerateArray().Select(v => v.GetSingle()).ToArray())
            .ToList();
    }

    private async Task<float[]> EmbedOneAsync(
        string baseUrl,
        string model,
        string text,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/api/embeddings",
            new { model, prompt = text },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ollama embeddings failed ({Status}): {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Ollama embeddings request failed ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("embedding", out var embedding))
            throw new InvalidOperationException("Ollama response missing embedding.");

        return embedding.EnumerateArray().Select(v => v.GetSingle()).ToArray();
    }
}

public sealed class EmbeddingGenerator : IEmbeddingGenerator
{
    private readonly IEnumerable<IEmbeddingProvider> _providers;
    private readonly KnowledgeOptions _options;

    public EmbeddingGenerator(IEnumerable<IEmbeddingProvider> providers)
        : this(providers, Microsoft.Extensions.Options.Options.Create(new KnowledgeOptions()))
    {
    }

    public EmbeddingGenerator(IEnumerable<IEmbeddingProvider> providers, IOptions<KnowledgeOptions> options)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<float[]> GenerateVectorAsync(
        string text,
        string? provider = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedProvider = string.IsNullOrWhiteSpace(provider) ? _options.DefaultEmbeddingProvider : provider;
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? _options.DefaultEmbeddingModel : model;
        var target = ResolveProvider(resolvedProvider);
        var vectors = await target.GenerateEmbeddingsAsync(new List<string> { text }, resolvedModel, cancellationToken);
        return vectors.FirstOrDefault()?.Values
               ?? throw new InvalidOperationException($"Embedding provider '{resolvedProvider}' returned no vector.");
    }

    public async Task<List<EmbeddingVector>> GenerateVectorsForChunksAsync(
        List<KnowledgeChunk> chunks,
        string? provider = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedProvider = string.IsNullOrWhiteSpace(provider) ? _options.DefaultEmbeddingProvider : provider;
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? _options.DefaultEmbeddingModel : model;
        var target = ResolveProvider(resolvedProvider);
        return await target.GenerateEmbeddingsAsync(chunks, resolvedModel, cancellationToken);
    }

    private IEmbeddingProvider ResolveProvider(string providerName)
        => _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"Embedding provider '{providerName}' is not registered.");
}
