using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Application.Chunkers;
using OCAP.Knowledge.Application.Parsers;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;
using OCAP.Knowledge.Infrastructure;
using OCAP.Knowledge.Infrastructure.Embeddings;
using OCAP.Knowledge.Infrastructure.Repositories;
using OCAP.Knowledge.Infrastructure.VectorDb;
using Xunit;

namespace OCAP.Knowledge.Tests;

public class KnowledgeTests
{
    [Fact]
    public async Task ParseAsync_AllDocumentTypes_ReturnsValidResult()
    {
        var parsers = new List<IDocumentParser>
        {
            new PdfDocumentParser(),
            new DocxDocumentParser(),
            new TxtDocumentParser(),
            new MarkdownDocumentParser(),
            new CsvDocumentParser(),
            new JsonDocumentParser(),
            new HtmlDocumentParser(),
            new XmlDocumentParser()
        };

        var factory = new DocumentParserFactory(parsers);

        foreach (var type in Enum.GetValues<DocumentType>())
        {
            var parser = factory.GetParser(type);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Sample content text for testing document ingestion."));
            var result = await parser.ParseAsync(stream, $"test_file.{type.ToString().ToLower()}", CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.ContentHash));
            Assert.NotNull(result.Text);
        }
    }

    [Theory]
    [InlineData(ChunkingStrategy.Sentence)]
    [InlineData(ChunkingStrategy.Paragraph)]
    [InlineData(ChunkingStrategy.Semantic)]
    [InlineData(ChunkingStrategy.SlidingWindow)]
    public void ChunkDocument_AllStrategies_CreatesValidChunks(ChunkingStrategy strategy)
    {
        var chunkers = new List<IChunker>
        {
            new SentenceChunker(),
            new ParagraphChunker(),
            new SemanticChunker(),
            new SlidingWindowChunker()
        };

        var factory = new ChunkerFactory(chunkers);
        var chunker = factory.GetChunker(strategy);
        const string sampleText = "Paragraph one content. Sentence two is here.\n\nParagraph two content. Sentence four is here.";

        var chunks = chunker.ChunkDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sampleText, chunkSize: 100, overlap: 10);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.False(string.IsNullOrWhiteSpace(c.Content)));
    }

    [Fact]
    public async Task VectorDatabase_TenantIsolation_EnforcesStrictSeparation()
    {
        var vectorDb = new InMemoryVectorDatabase(NullLogger<InMemoryVectorDatabase>.Instance);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var vectorA = new[]
        {
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 4, [1.0f, 0.0f, 0.0f, 0.0f], KnowledgeBaseId: Guid.NewGuid())
        };
        var vectorB = new[]
        {
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 4, [0.0f, 1.0f, 0.0f, 0.0f], KnowledgeBaseId: Guid.NewGuid())
        };

        await vectorDb.UpsertVectorsAsync(tenantA, vectorA);
        await vectorDb.UpsertVectorsAsync(tenantB, vectorB);

        var searchA = await vectorDb.SearchVectorsAsync(tenantA, [1.0f, 0.0f, 0.0f, 0.0f], topK: 10, minScore: 0.1);
        var searchB = await vectorDb.SearchVectorsAsync(tenantB, [1.0f, 0.0f, 0.0f, 0.0f], topK: 10, minScore: 0.1);

        Assert.Single(searchA);
        Assert.Empty(searchB);
    }

    [Fact]
    public async Task VectorDatabase_CosineSimilarity_ReturnsTopKOrderedResults()
    {
        var vectorDb = new InMemoryVectorDatabase(NullLogger<InMemoryVectorDatabase>.Instance);
        var tenantId = Guid.NewGuid();
        var kbId = Guid.NewGuid();

        await vectorDb.UpsertVectorsAsync(tenantId,
        [
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 3, [1f, 0f, 0f], KnowledgeBaseId: kbId),
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 3, [0.9f, 0.1f, 0f], KnowledgeBaseId: kbId),
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 3, [0f, 1f, 0f], KnowledgeBaseId: kbId)
        ]);

        var results = await vectorDb.SearchVectorsAsync(tenantId, [1f, 0f, 0f], topK: 2, minScore: 0.0, knowledgeBaseId: kbId);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Score >= results[1].Score);
        Assert.True(results[0].Score > 0.99);
    }

    [Fact]
    public async Task VectorDatabase_FiltersByKnowledgeBaseAndTags()
    {
        var vectorDb = new InMemoryVectorDatabase(NullLogger<InMemoryVectorDatabase>.Instance);
        var tenantId = Guid.NewGuid();
        var kbA = Guid.NewGuid();
        var kbB = Guid.NewGuid();

        await vectorDb.UpsertVectorsAsync(tenantId,
        [
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 2, [1f, 0f], KnowledgeBaseId: kbA, Tags: ["alpha"]),
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 2, [1f, 0f], KnowledgeBaseId: kbA, Tags: ["beta"]),
            new EmbeddingVector(Guid.NewGuid(), "Fake", "unit", 2, [1f, 0f], KnowledgeBaseId: kbB, Tags: ["alpha"])
        ]);

        var byKb = await vectorDb.SearchVectorsAsync(tenantId, [1f, 0f], topK: 10, minScore: 0.1, knowledgeBaseId: kbA);
        var byTag = await vectorDb.SearchVectorsAsync(tenantId, [1f, 0f], topK: 10, minScore: 0.1, knowledgeBaseId: kbA, tags: ["alpha"]);

        Assert.Equal(2, byKb.Count);
        Assert.Single(byTag);
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var similarity = InMemoryVectorDatabase.CosineSimilarity([1f, 2f, 3f], [1f, 2f, 3f]);
        Assert.Equal(1.0, similarity, precision: 5);
    }

    [Fact]
    public async Task KnowledgeService_UploadAndSearch_FlowSucceeds()
    {
        var kbRepo = new InMemoryKnowledgeBaseRepository();
        var docRepo = new InMemoryKnowledgeDocumentRepository();
        var chunkRepo = new InMemoryKnowledgeChunkRepository();
        var jobRepo = new InMemoryDocumentProcessingJobRepository();

        var parsers = new List<IDocumentParser> { new TxtDocumentParser() };
        var parserFactory = new DocumentParserFactory(parsers);

        var chunkers = new List<IChunker> { new ParagraphChunker() };
        var chunkerFactory = new ChunkerFactory(chunkers);

        var embeddingProvider = new FakeEmbeddingProvider();
        var generator = new EmbeddingGenerator(
            [embeddingProvider],
            Options.Create(new KnowledgeOptions
            {
                DefaultEmbeddingProvider = "Fake",
                DefaultEmbeddingModel = "unit"
            }));
        var vectorDb = new InMemoryVectorDatabase(NullLogger<InMemoryVectorDatabase>.Instance);

        var validator = new FileUploadValidator();
        var retriever = new KnowledgeRetriever(vectorDb, generator, chunkRepo, docRepo, NullLogger<KnowledgeRetriever>.Instance);
        var service = new KnowledgeService(kbRepo, docRepo, chunkRepo, jobRepo, parserFactory, chunkerFactory, generator, vectorDb, retriever, validator, NullLogger<KnowledgeService>.Instance);

        var tenantId = Guid.NewGuid();
        var kb = await service.CreateKnowledgeBaseAsync(tenantId, "Base Test", "Descripción de prueba", ChunkingStrategy.Paragraph, VectorDbProviderType.PgVector);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Manual de usuario para configurar OCAP RAG."));

        var doc = await service.UploadDocumentAsync(tenantId, kb.Id, stream, "manual.txt", DocumentType.Txt, DocumentCategory.Technical);
        var searchResults = await service.SearchAsync(tenantId, kb.Id, "OCAP RAG", SearchStrategyType.Hybrid);

        Assert.NotNull(doc);
        Assert.Equal(DocumentStatus.Indexed, doc.Status);
        Assert.NotEmpty(searchResults);
    }

    [Theory]
    [InlineData("../../secret.txt")]
    [InlineData("..\\..\\windows\\system32\\cmd.exe")]
    [InlineData("/etc/passwd")]
    public void FileUploadValidator_PathTraversal_SanitizesFileName(string maliciousFileName)
    {
        var validator = new FileUploadValidator();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Valid content text"));

        var result = validator.ValidateFile(stream, maliciousFileName, "text/plain", 1024);

        Assert.DoesNotContain("..", result.SanitizedFileName);
        Assert.DoesNotContain("/", result.SanitizedFileName);
        Assert.DoesNotContain("\\", result.SanitizedFileName);
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.sh")]
    [InlineData("library.dll")]
    [InlineData("image.png")]
    public void FileUploadValidator_DisallowedExtension_ReturnsInvalid(string invalidFileName)
    {
        var validator = new FileUploadValidator();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Valid content text"));

        var result = validator.ValidateFile(stream, invalidFileName, "application/octet-stream", 1024);

        Assert.False(result.IsValid);
        Assert.Contains("no está permitida", result.ErrorMessage);
    }

    [Fact]
    public void FileUploadValidator_OversizedFile_ReturnsInvalid()
    {
        var validator = new FileUploadValidator();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Valid content text"));
        long smallLimitBytes = 5;

        var result = validator.ValidateFile(stream, "document.txt", "text/plain", smallLimitBytes);

        Assert.False(result.IsValid);
        Assert.Contains("excede el tamaño", result.ErrorMessage);
    }

    [Fact]
    public void FileUploadValidator_ValidFile_ReturnsSHA256Checksum()
    {
        var validator = new FileUploadValidator();
        byte[] bytes = Encoding.UTF8.GetBytes("OCAP Enterprise RAG Security Validation test content.");
        using var stream = new MemoryStream(bytes);

        var result = validator.ValidateFile(stream, "report.pdf", "application/pdf", 1024 * 1024);
        var hash = validator.ComputeSha256Hash(stream);

        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("report.pdf", result.SanitizedFileName);
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal(64, hash.Length);
    }
}

public class EmbeddingProviderTests
{
    [Fact]
    public async Task OpenAiEmbeddingProvider_CallsRealEmbeddingsEndpoint()
    {
        var handler = new StubHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/embeddings", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            Assert.Equal("Bearer test-key", request.Headers.Authorization!.ToString());

            var payload = await request.Content!.ReadAsStringAsync();
            Assert.Contains("text-embedding-3-small", payload);

            var body = JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new { index = 0, embedding = new[] { 0.1f, 0.2f, 0.3f } }
                }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var http = new HttpClient(handler);
        var options = Options.Create(new KnowledgeOptions
        {
            OpenAI = new EmbeddingProviderOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.openai.com/v1",
                DefaultModel = "text-embedding-3-small"
            }
        });

        var provider = new OpenAiEmbeddingProvider(http, options, NullLogger<OpenAiEmbeddingProvider>.Instance);
        var vectors = await provider.GenerateEmbeddingsAsync(["hello world"], "text-embedding-3-small");

        Assert.Single(vectors);
        Assert.Equal([0.1f, 0.2f, 0.3f], vectors[0].Values);
        Assert.Equal("OpenAI", vectors[0].Provider);
    }

    [Fact]
    public async Task GeminiEmbeddingProvider_CallsEmbedContentEndpoint()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var body = JsonSerializer.Serialize(new
            {
                embedding = new { values = new[] { 0.5f, 0.25f } }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        var http = new HttpClient(handler);
        var options = Options.Create(new KnowledgeOptions
        {
            EmbeddingDimensions = 2,
            Gemini = new EmbeddingProviderOptions
            {
                ApiKey = "gemini-key",
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
                DefaultModel = "text-embedding-004"
            }
        });

        var provider = new GeminiEmbeddingProvider(http, options, NullLogger<GeminiEmbeddingProvider>.Instance);
        var vectors = await provider.GenerateEmbeddingsAsync(["rag"], "text-embedding-004");

        Assert.Single(vectors);
        Assert.Equal([0.5f, 0.25f], vectors[0].Values);
        Assert.Equal("Gemini", vectors[0].Provider);
    }

    [Fact]
    public async Task OllamaEmbeddingProvider_CallsApiEmbedEndpoint()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var body = JsonSerializer.Serialize(new
            {
                embeddings = new[] { new[] { 0.7f, 0.1f, 0.2f } }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        var http = new HttpClient(handler);
        var options = Options.Create(new KnowledgeOptions
        {
            Ollama = new EmbeddingProviderOptions
            {
                BaseUrl = "http://localhost:11434",
                DefaultModel = "nomic-embed-text"
            }
        });

        var provider = new OllamaEmbeddingProvider(http, options, NullLogger<OllamaEmbeddingProvider>.Instance);
        var vectors = await provider.GenerateEmbeddingsAsync(["local"], "nomic-embed-text");

        Assert.Single(vectors);
        Assert.Equal([0.7f, 0.1f, 0.2f], vectors[0].Values);
        Assert.Equal("Ollama", vectors[0].Provider);
    }

    [Fact]
    public async Task OpenAiEmbeddingProvider_MissingApiKey_Throws()
    {
        var previous = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        try
        {
            var options = Options.Create(new KnowledgeOptions
            {
                OpenAI = new EmbeddingProviderOptions { ApiKey = "", BaseUrl = "https://api.openai.com/v1" }
            });
            var provider = new OpenAiEmbeddingProvider(new HttpClient(), options, NullLogger<OpenAiEmbeddingProvider>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.GenerateEmbeddingsAsync(["x"], "text-embedding-3-small"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", previous);
        }
    }
}

public class KnowledgeDiTests
{
    [Fact]
    public void AddKnowledgeModule_InMemory_UsesInMemoryVectorDatabase()
    {
        var config = BuildKnowledgeConfig("""
        {
          "Knowledge": {
            "UseInMemory": true,
            "VectorStore": "InMemory"
          }
        }
        """);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKnowledgeModule(config);
        using var sp = services.BuildServiceProvider();

        var vectorDb = sp.GetRequiredService<IVectorDatabase>();
        Assert.IsType<InMemoryVectorDatabase>(vectorDb);
        Assert.Equal(VectorDbProviderType.InMemory, vectorDb.ProviderType);
    }

    [Fact]
    public void AddKnowledgeModule_PgVector_RegistersPgVectorDatabase()
    {
        var config = BuildKnowledgeConfig("""
        {
          "Knowledge": {
            "UseInMemory": false,
            "VectorStore": "PgVector"
          }
        }
        """);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKnowledgeModule(config);

        Assert.Contains(services, d => d.ServiceType == typeof(IVectorDatabase) && d.ImplementationType == typeof(PgVectorDatabase));
        Assert.Contains(services, d => d.ImplementationType == typeof(PgVectorStartupValidator));
    }

    private static IConfiguration BuildKnowledgeConfig(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ocap-knowledge-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return new ConfigurationBuilder()
            .AddJsonFile(path, optional: false, reloadOnChange: false)
            .Build();
    }
}

/// <summary>
/// Deterministic test double — not a production embedding provider.
/// </summary>
internal sealed class FakeEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderName => "Fake";

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<KnowledgeChunk> chunks, string model, CancellationToken cancellationToken = default)
    {
        var vectors = chunks.Select(c => new EmbeddingVector(
            c.Id,
            ProviderName,
            model,
            8,
            TextToVector(c.Content),
            c.DocumentId,
            c.KnowledgeBaseId,
            c.TenantId,
            c.MetadataJson)).ToList();
        return Task.FromResult(vectors);
    }

    public Task<List<EmbeddingVector>> GenerateEmbeddingsAsync(List<string> texts, string model, CancellationToken cancellationToken = default)
    {
        var vectors = texts.Select(t => new EmbeddingVector(Guid.NewGuid(), ProviderName, model, 8, TextToVector(t))).ToList();
        return Task.FromResult(vectors);
    }

    private static float[] TextToVector(string text)
    {
        var values = new float[8];
        if (string.IsNullOrEmpty(text)) return values;
        foreach (var ch in text.ToLowerInvariant())
            values[ch % 8] += 1f;

        var norm = MathF.Sqrt(values.Sum(v => v * v));
        if (norm > 0)
        {
            for (var i = 0; i < values.Length; i++)
                values[i] /= norm;
        }

        return values;
    }
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request, cancellationToken);
}
