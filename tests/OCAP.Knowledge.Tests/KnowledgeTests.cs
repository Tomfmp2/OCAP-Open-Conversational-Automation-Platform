using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Application.Chunkers;
using OCAP.Knowledge.Application.Parsers;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.Enums;
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
        // Arrange
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
            
            // Act
            var result = await parser.ParseAsync(stream, $"test_file.{type.ToString().ToLower()}", CancellationToken.None);

            // Assert
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
        // Arrange
        var chunkers = new List<IChunker>
        {
            new SentenceChunker(),
            new ParagraphChunker(),
            new SemanticChunker(),
            new SlidingWindowChunker()
        };

        var factory = new ChunkerFactory(chunkers);
        var chunker = factory.GetChunker(strategy);
        string sampleText = "Paragraph one content. Sentence two is here.\n\nParagraph two content. Sentence four is here.";

        // Act
        var chunks = chunker.ChunkDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sampleText, chunkSize: 100, overlap: 10);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.False(string.IsNullOrWhiteSpace(c.Content)));
    }

    [Fact]
    public async Task VectorDatabase_TenantIsolation_EnforcesStrictSeparation()
    {
        // Arrange
        var vectorDb = new PgVectorStorage(NullLogger<PgVectorStorage>.Instance);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var vectorA = new[] { new Domain.ValueObjects.EmbeddingVector(Guid.NewGuid(), "OpenAI", "text-embedding-3-small", 4, new float[] { 1.0f, 0.0f, 0.0f, 0.0f }) };
        var vectorB = new[] { new Domain.ValueObjects.EmbeddingVector(Guid.NewGuid(), "OpenAI", "text-embedding-3-small", 4, new float[] { 0.0f, 1.0f, 0.0f, 0.0f }) };

        // Act
        await vectorDb.UpsertVectorsAsync(tenantA, vectorA);
        await vectorDb.UpsertVectorsAsync(tenantB, vectorB);

        var searchA = await vectorDb.SearchVectorsAsync(tenantA, new float[] { 1.0f, 0.0f, 0.0f, 0.0f }, topK: 10, minScore: 0.1);
        var searchB = await vectorDb.SearchVectorsAsync(tenantB, new float[] { 1.0f, 0.0f, 0.0f, 0.0f }, topK: 10, minScore: 0.1);

        // Assert
        Assert.Single(searchA);
        Assert.Empty(searchB); // Tenant B must not see Tenant A's vectors!
    }

    [Fact]
    public async Task KnowledgeService_UploadAndSearch_FlowSucceeds()
    {
        // Arrange
        var kbRepo = new InMemoryKnowledgeBaseRepository();
        var docRepo = new InMemoryKnowledgeDocumentRepository();
        var chunkRepo = new InMemoryKnowledgeChunkRepository();
        var jobRepo = new InMemoryDocumentProcessingJobRepository();

        var parsers = new List<IDocumentParser> { new TxtDocumentParser() };
        var parserFactory = new DocumentParserFactory(parsers);

        var chunkers = new List<IChunker> { new ParagraphChunker() };
        var chunkerFactory = new ChunkerFactory(chunkers);

        var embeddingProvider = new OpenAiEmbeddingProvider(new HttpClient(), NullLogger<OpenAiEmbeddingProvider>.Instance);
        var generator = new EmbeddingGenerator(new[] { embeddingProvider });
        var vectorDb = new PgVectorStorage(NullLogger<PgVectorStorage>.Instance);

        var validator = new FileUploadValidator();
        var retriever = new KnowledgeRetriever(vectorDb, generator, chunkRepo, docRepo, NullLogger<KnowledgeRetriever>.Instance);
        var service = new KnowledgeService(kbRepo, docRepo, chunkRepo, jobRepo, parserFactory, chunkerFactory, generator, vectorDb, retriever, validator, NullLogger<KnowledgeService>.Instance);

        var tenantId = Guid.NewGuid();
        var kb = await service.CreateKnowledgeBaseAsync(tenantId, "Base Test", "Descripción de prueba", ChunkingStrategy.Paragraph, VectorDbProviderType.PgVector);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Manual de usuario para configurar OCAP RAG."));

        // Act
        var doc = await service.UploadDocumentAsync(tenantId, kb.Id, stream, "manual.txt", DocumentType.Txt, DocumentCategory.Technical);
        var searchResults = await service.SearchAsync(tenantId, kb.Id, "OCAP RAG", SearchStrategyType.Hybrid);

        // Assert
        Assert.NotNull(doc);
        Assert.Equal(DocumentStatus.Indexed, doc.Status);
        Assert.NotEmpty(searchResults);
    }
}
