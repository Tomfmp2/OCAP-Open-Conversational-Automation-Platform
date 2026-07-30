using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Application.Chunkers;
using OCAP.Knowledge.Application.Parsers;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Infrastructure.Embeddings;
using OCAP.Knowledge.Infrastructure.Repositories;
using OCAP.Knowledge.Infrastructure.Telemetry;
using OCAP.Knowledge.Infrastructure.VectorDb;

namespace OCAP.Knowledge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKnowledgeModule(this IServiceCollection services)
        => services.AddKnowledgeModule(configuration: null);

    public static IServiceCollection AddKnowledgeModule(this IServiceCollection services, IConfiguration? configuration)
    {
        if (configuration is not null)
        {
            services.Configure<KnowledgeOptions>(configuration.GetSection(KnowledgeOptions.SectionName));
            services.PostConfigure<KnowledgeOptions>(options =>
            {
                ApplyLegacyAiProviderFallbacks(options, configuration);
                if (string.Equals(configuration["UseInMemory"], "true", StringComparison.OrdinalIgnoreCase))
                    options.UseInMemory = true;
            });
        }
        else
        {
            services.AddOptions<KnowledgeOptions>();
        }

        services.AddSingleton<IKnowledgeTelemetry, KnowledgeTelemetry>();

        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, TxtDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, CsvDocumentParser>();
        services.AddSingleton<IDocumentParser, JsonDocumentParser>();
        services.AddSingleton<IDocumentParser, HtmlDocumentParser>();
        services.AddSingleton<IDocumentParser, XmlDocumentParser>();
        services.AddSingleton<IDocumentParserFactory, DocumentParserFactory>();

        services.AddSingleton<IChunker, SentenceChunker>();
        services.AddSingleton<IChunker, ParagraphChunker>();
        services.AddSingleton<IChunker, SemanticChunker>();
        services.AddSingleton<IChunker, SlidingWindowChunker>();
        services.AddSingleton<IChunkerFactory, ChunkerFactory>();

        services.AddHttpClient(nameof(OpenAiEmbeddingProvider));
        services.AddHttpClient(nameof(GeminiEmbeddingProvider));
        services.AddHttpClient(nameof(OllamaEmbeddingProvider));

        services.AddSingleton<IEmbeddingProvider>(sp =>
            new OpenAiEmbeddingProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAiEmbeddingProvider)),
                sp.GetRequiredService<IOptions<KnowledgeOptions>>(),
                sp.GetRequiredService<ILogger<OpenAiEmbeddingProvider>>()));

        services.AddSingleton<IEmbeddingProvider>(sp =>
            new GeminiEmbeddingProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GeminiEmbeddingProvider)),
                sp.GetRequiredService<IOptions<KnowledgeOptions>>(),
                sp.GetRequiredService<ILogger<GeminiEmbeddingProvider>>()));

        services.AddSingleton<IEmbeddingProvider>(sp =>
            new OllamaEmbeddingProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OllamaEmbeddingProvider)),
                sp.GetRequiredService<IOptions<KnowledgeOptions>>(),
                sp.GetRequiredService<ILogger<OllamaEmbeddingProvider>>()));

        services.AddSingleton<IEmbeddingGenerator, EmbeddingGenerator>();

        var knowledgeOptions = configuration?.GetSection(KnowledgeOptions.SectionName).Get<KnowledgeOptions>() ?? new KnowledgeOptions();
        if (configuration is not null)
            ApplyLegacyAiProviderFallbacks(knowledgeOptions, configuration);

        var globalUseInMemory = configuration is not null
            && string.Equals(configuration["UseInMemory"], "true", StringComparison.OrdinalIgnoreCase);

        var useInMemory = knowledgeOptions.IsInMemoryVectorStore || globalUseInMemory;

        if (useInMemory)
        {
            services.AddSingleton<IKnowledgeBaseRepository, InMemoryKnowledgeBaseRepository>();
            services.AddSingleton<IKnowledgeDocumentRepository, InMemoryKnowledgeDocumentRepository>();
            services.AddSingleton<IKnowledgeChunkRepository, InMemoryKnowledgeChunkRepository>();
            services.AddSingleton<IDocumentProcessingJobRepository, InMemoryDocumentProcessingJobRepository>();
            services.AddSingleton<IVectorDatabase, InMemoryVectorDatabase>();
        }
        else
        {
            services.AddScoped<IKnowledgeBaseRepository, EfKnowledgeBaseRepository>();
            services.AddScoped<IKnowledgeDocumentRepository, EfKnowledgeDocumentRepository>();
            services.AddScoped<IKnowledgeChunkRepository, EfKnowledgeChunkRepository>();
            services.AddScoped<IDocumentProcessingJobRepository, EfDocumentProcessingJobRepository>();
            services.AddScoped<IVectorDatabase, PgVectorDatabase>();
            services.AddHostedService<PgVectorStartupValidator>();
        }

        services.AddSingleton<IFileUploadValidator, FileUploadValidator>();
        services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();
        services.AddScoped<KnowledgeService>();

        return services;
    }

    private static void ApplyLegacyAiProviderFallbacks(KnowledgeOptions options, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
            options.OpenAI.ApiKey = configuration["AiProviders:OpenAI:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(options.OpenAI.BaseUrl))
            options.OpenAI.BaseUrl = configuration["AiProviders:OpenAI:BaseUrl"] ?? options.OpenAI.BaseUrl;

        if (string.IsNullOrWhiteSpace(options.Gemini.ApiKey))
            options.Gemini.ApiKey = configuration["AiProviders:Gemini:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(options.Gemini.BaseUrl))
            options.Gemini.BaseUrl = configuration["AiProviders:Gemini:BaseUrl"] ?? options.Gemini.BaseUrl;

        if (string.IsNullOrWhiteSpace(options.Ollama.BaseUrl))
            options.Ollama.BaseUrl = configuration["AiProviders:Ollama:BaseUrl"] ?? options.Ollama.BaseUrl;
    }
}

/// <summary>
/// Fail-fast when PgVector is required but the PostgreSQL extension is missing.
/// </summary>
public sealed class PgVectorStartupValidator : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly IOptions<KnowledgeOptions> _options;
    private readonly ILogger<PgVectorStartupValidator> _logger;

    public PgVectorStartupValidator(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        IOptions<KnowledgeOptions> options,
        ILogger<PgVectorStartupValidator> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        if (_environment.IsProduction() && options.IsInMemoryVectorStore)
        {
            throw new InvalidOperationException(
                "In-memory Knowledge/RAG storage is forbidden in Production. Set Knowledge:UseInMemory=false and Knowledge:VectorStore=PgVector.");
        }

        if (options.IsInMemoryVectorStore)
            return;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<DbContext>();
        if (dbContext is null)
        {
            _logger.LogWarning("PgVectorStartupValidator: DbContext is not registered; extension check deferred.");
            return;
        }

        if (!dbContext.Database.IsRelational())
            return;

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM pg_extension WHERE extname = 'vector'";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "PgVector is enabled but the PostgreSQL 'vector' extension is unavailable. Execute: CREATE EXTENSION IF NOT EXISTS vector;");
            }

            _logger.LogInformation("PgVector extension validated successfully.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex) when (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
        {
            _logger.LogWarning(ex,
                "PgVector extension validation skipped due to connectivity error in {Environment}.",
                _environment.EnvironmentName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
