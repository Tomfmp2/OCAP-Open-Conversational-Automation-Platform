using Microsoft.Extensions.Logging;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Application.Services;

public class KnowledgeRetriever : IKnowledgeRetriever
{
    private readonly IVectorDatabase _vectorDatabase;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IKnowledgeChunkRepository _chunkRepository;
    private readonly IKnowledgeDocumentRepository _documentRepository;
    private readonly IKnowledgeTelemetry? _telemetry;
    private readonly ILogger<KnowledgeRetriever> _logger;

    public KnowledgeRetriever(
        IVectorDatabase vectorDatabase,
        IEmbeddingGenerator embeddingGenerator,
        IKnowledgeChunkRepository chunkRepository,
        IKnowledgeDocumentRepository documentRepository,
        ILogger<KnowledgeRetriever> logger,
        IKnowledgeTelemetry? telemetry = null)
    {
        _vectorDatabase = vectorDatabase ?? throw new ArgumentNullException(nameof(vectorDatabase));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetry = telemetry;
    }

    public Task<List<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId,
        string query,
        SearchStrategyType strategy = SearchStrategyType.Hybrid,
        int topK = 5,
        double minScore = 0.5,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(tenantId, null, query, strategy, topK, minScore, cancellationToken);
    }

    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string query,
        SearchStrategyType strategy = SearchStrategyType.Hybrid,
        int topK = 5,
        double minScore = 0.5,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for retrieval isolation.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(query)) return new List<KnowledgeSearchResult>();

        _logger.LogInformation("Ejecutando Knowledge Retrieval ({Strategy}) para Tenant {TenantId}. Query: {Query}", strategy, tenantId, query);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        List<KnowledgeSearchResult> results;
        try
        {
            switch (strategy)
            {
                case SearchStrategyType.Similarity:
                case SearchStrategyType.Semantic:
                    results = await ExecuteVectorSearchAsync(tenantId, knowledgeBaseId, query, topK, minScore, cancellationToken);
                    break;

                case SearchStrategyType.Keyword:
                    results = await ExecuteKeywordSearchAsync(tenantId, knowledgeBaseId, query, topK, cancellationToken);
                    break;

                case SearchStrategyType.Hybrid:
                default:
                    var vectorResults = await ExecuteVectorSearchAsync(tenantId, knowledgeBaseId, query, topK, minScore, cancellationToken);
                    var keywordResults = await ExecuteKeywordSearchAsync(tenantId, knowledgeBaseId, query, topK, cancellationToken);
                    results = MergeHybridResults(vectorResults, keywordResults, topK);
                    break;
            }

            stopwatch.Stop();
            double topScore = results.Count > 0 ? results.Max(r => r.Score) : 0.0;
            _telemetry?.RecordRetrievalExecuted(tenantId, strategy.ToString(), topK, results.Count, topScore, stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetry?.RecordError(tenantId, "SearchAsync", ex.Message);
            throw;
        }
    }

    private async Task<List<KnowledgeSearchResult>> ExecuteVectorSearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string query,
        int topK,
        double minScore,
        CancellationToken cancellationToken)
    {
        var queryVector = await _embeddingGenerator.GenerateVectorAsync(query, cancellationToken: cancellationToken);
        return await _vectorDatabase.SearchVectorsAsync(
            tenantId,
            queryVector,
            topK,
            minScore,
            knowledgeBaseId,
            tags: null,
            cancellationToken);
    }

    private async Task<List<KnowledgeSearchResult>> ExecuteKeywordSearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string query,
        int topK,
        CancellationToken cancellationToken)
    {
        // Optimized keyword scoring over tenant chunks (optionally scoped to a knowledge base).
        var results = new List<KnowledgeSearchResult>();
        var queryTerms = new HashSet<string>(query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (queryTerms.Count == 0) return results;

        var kbFilter = knowledgeBaseId ?? Guid.Empty;
        var docs = await _documentRepository.GetByKnowledgeBaseAsync(kbFilter, tenantId, cancellationToken);
        foreach (var doc in docs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunks = await _chunkRepository.GetByDocumentAsync(doc.Id, tenantId, cancellationToken);
            foreach (var chunk in chunks)
            {
                var contentLower = chunk.Content.ToLowerInvariant();
                int matchCount = 0;
                var highlights = new List<string>(queryTerms.Count);

                foreach (var term in queryTerms)
                {
                    if (contentLower.Contains(term))
                    {
                        matchCount++;
                        highlights.Add(term);
                    }
                }

                if (matchCount > 0)
                {
                    double score = (double)matchCount / queryTerms.Count;
                    results.Add(new KnowledgeSearchResult(
                        chunk.Id,
                        chunk.DocumentId,
                        doc.Title,
                        chunk.Content,
                        score,
                        1.0 - score,
                        chunk.MetadataJson,
                        highlights
                    ));
                }
            }
        }

        return results.OrderByDescending(r => r.Score).Take(topK).ToList();
    }

    private static List<KnowledgeSearchResult> MergeHybridResults(List<KnowledgeSearchResult> vectorResults, List<KnowledgeSearchResult> keywordResults, int topK)
    {
        var dict = new Dictionary<Guid, KnowledgeSearchResult>();

        foreach (var v in vectorResults)
        {
            dict[v.ChunkId] = v;
        }

        foreach (var k in keywordResults)
        {
            if (dict.TryGetValue(k.ChunkId, out var existing))
            {
                // Hybrid RRF (Reciprocal Rank Fusion) boost
                double boostedScore = (existing.Score * 0.7) + (k.Score * 0.3);
                dict[k.ChunkId] = existing with { Score = boostedScore };
            }
            else
            {
                dict[k.ChunkId] = k;
            }
        }

        return dict.Values.OrderByDescending(r => r.Score).Take(topK).ToList();
    }
}
