using Microsoft.Extensions.DependencyInjection;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Application.Chunkers;
using OCAP.Knowledge.Application.Parsers;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Infrastructure.Embeddings;
using OCAP.Knowledge.Infrastructure.Repositories;
using OCAP.Knowledge.Infrastructure.VectorDb;

namespace OCAP.Knowledge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKnowledgeModule(this IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<IKnowledgeBaseRepository, InMemoryKnowledgeBaseRepository>();
        services.AddSingleton<IKnowledgeDocumentRepository, InMemoryKnowledgeDocumentRepository>();
        services.AddSingleton<IKnowledgeChunkRepository, InMemoryKnowledgeChunkRepository>();
        services.AddSingleton<IDocumentProcessingJobRepository, InMemoryDocumentProcessingJobRepository>();

        // Document Parsers
        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, TxtDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, CsvDocumentParser>();
        services.AddSingleton<IDocumentParser, JsonDocumentParser>();
        services.AddSingleton<IDocumentParser, HtmlDocumentParser>();
        services.AddSingleton<IDocumentParser, XmlDocumentParser>();
        services.AddSingleton<IDocumentParserFactory, DocumentParserFactory>();

        // Chunkers
        services.AddSingleton<IChunker, SentenceChunker>();
        services.AddSingleton<IChunker, ParagraphChunker>();
        services.AddSingleton<IChunker, SemanticChunker>();
        services.AddSingleton<IChunker, SlidingWindowChunker>();
        services.AddSingleton<IChunkerFactory, ChunkerFactory>();

        // Embeddings
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IEmbeddingProvider, OpenAiEmbeddingProvider>();
        services.AddSingleton<IEmbeddingProvider, GeminiEmbeddingProvider>();
        services.AddSingleton<IEmbeddingProvider, OllamaEmbeddingProvider>();
        services.AddSingleton<IEmbeddingGenerator, EmbeddingGenerator>();

        // Vector Databases
        services.AddSingleton<PgVectorStorage>();
        services.AddSingleton<QdrantVectorStorage>();
        services.AddSingleton<ChromaVectorStorage>();
        services.AddSingleton<PineconeVectorStorage>();
        services.AddSingleton<IVectorDatabase, PgVectorStorage>();

        // Services, Validator & Retriever
        services.AddSingleton<IFileUploadValidator, FileUploadValidator>();
        services.AddSingleton<IKnowledgeRetriever, KnowledgeRetriever>();
        services.AddSingleton<KnowledgeService>();

        return services;
    }
}
