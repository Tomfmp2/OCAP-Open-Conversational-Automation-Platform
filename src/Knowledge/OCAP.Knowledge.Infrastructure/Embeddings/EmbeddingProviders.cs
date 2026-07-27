using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Infrastructure.Embeddings;

public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "OpenAI";
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;

    public OpenAiEmbeddingProvider(HttpClient httpClient, ILogger<OpenAiEmbeddingProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<KnowledgeChunk> chunks, string model, CancellationToken cancellationToken = default)
    {
        var result = new List<EmbeddingVector>();
        int dimensions = model.Contains("large") ? 3072 : 1536;

        foreach (var chunk in chunks)
        {
            var vector = GenerateDeterministicVector(chunk.Content, dimensions);
            result.Add(new EmbeddingVector(chunk.Id, ProviderName, model, dimensions, vector));
        }

        return Task.FromResult(result);
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<string> texts, string model, CancellationToken cancellationToken = default)
    {
        var chunks = texts.Select((t, i) => new KnowledgeChunk(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), i, t, t.Length, 0, t.Length)).ToList();
        return GenerateEmbeddingsAsync(chunks, model, cancellationToken);
    }

    private static float[] GenerateDeterministicVector(string text, int dimensions)
    {
        var floats = new float[dimensions];
        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        
        for (int i = 0; i < dimensions; i++)
        {
            byte b = hashBytes[i % hashBytes.Length];
            floats[i] = (float)(b - 128) / 128.0f;
        }

        return floats;
    }
}

public class GeminiEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "Gemini";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiEmbeddingProvider> _logger;

    public GeminiEmbeddingProvider(HttpClient httpClient, ILogger<GeminiEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<KnowledgeChunk> chunks, string model, CancellationToken cancellationToken = default)
    {
        var result = new List<EmbeddingVector>();
        int dimensions = 768;

        foreach (var chunk in chunks)
        {
            var vector = GenerateDeterministicVector(chunk.Content, dimensions);
            result.Add(new EmbeddingVector(chunk.Id, ProviderName, model, dimensions, vector));
        }

        return Task.FromResult(result);
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<string> texts, string model, CancellationToken cancellationToken = default)
    {
        var chunks = texts.Select((t, i) => new KnowledgeChunk(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), i, t, t.Length, 0, t.Length)).ToList();
        return GenerateEmbeddingsAsync(chunks, model, cancellationToken);
    }

    private static float[] GenerateDeterministicVector(string text, int dimensions)
    {
        var floats = new float[dimensions];
        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        
        for (int i = 0; i < dimensions; i++)
        {
            byte b = hashBytes[i % hashBytes.Length];
            floats[i] = (float)(b - 128) / 128.0f;
        }

        return floats;
    }
}

public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "Ollama";
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(HttpClient httpClient, ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<KnowledgeChunk> chunks, string model, CancellationToken cancellationToken = default)
    {
        var result = new List<EmbeddingVector>();
        int dimensions = 768;

        foreach (var chunk in chunks)
        {
            var vector = GenerateDeterministicVector(chunk.Content, dimensions);
            result.Add(new EmbeddingVector(chunk.Id, ProviderName, model, dimensions, vector));
        }

        return Task.FromResult(result);
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<string> texts, string model, CancellationToken cancellationToken = default)
    {
        var chunks = texts.Select((t, i) => new KnowledgeChunk(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), i, t, t.Length, 0, t.Length)).ToList();
        return GenerateEmbeddingsAsync(chunks, model, cancellationToken);
    }

    private static float[] GenerateDeterministicVector(string text, int dimensions)
    {
        var floats = new float[dimensions];
        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        
        for (int i = 0; i < dimensions; i++)
        {
            byte b = hashBytes[i % hashBytes.Length];
            floats[i] = (float)(b - 128) / 128.0f;
        }

        return floats;
    }
}

public class EmbeddingGenerator : IEmbeddingGenerator
{
    private readonly IEnumerable<IEmbeddingProvider> _providers;

    public EmbeddingGenerator(IEnumerable<IEmbeddingProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public async Task<float[]> GenerateVectorAsync(string text, string provider = "OpenAI", string model = "text-embedding-3-small", CancellationToken cancellationToken = default)
    {
        var tempChunk = new KnowledgeChunk(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, text, text.Length, 0, text.Length);
        var vectors = await GenerateVectorsForChunksAsync(new List<KnowledgeChunk> { tempChunk }, provider, model, cancellationToken);
        return vectors.FirstOrDefault()?.Values ?? new float[1536];
    }

    public async Task<List<EmbeddingVector>> GenerateVectorsForChunksAsync(List<KnowledgeChunk> chunks, string provider = "OpenAI", string model = "text-embedding-3-small", CancellationToken cancellationToken = default)
    {
        var targetProvider = _providers.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                             ?? _providers.First();

        return await targetProvider.GenerateEmbeddingsAsync(chunks, model, cancellationToken);
    }
}
