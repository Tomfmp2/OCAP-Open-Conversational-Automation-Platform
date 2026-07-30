using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;
using Pgvector;

namespace OCAP.Knowledge.Infrastructure.VectorDb;

/// <summary>
/// In-memory vector store for Development/Testing when explicitly configured.
/// Production must never use this implementation.
/// </summary>
public class InMemoryVectorDatabase : IVectorDatabase
{
    public VectorDbProviderType ProviderType => VectorDbProviderType.InMemory;

    private readonly Dictionary<Guid, List<StoredVector>> _storage = new();
    private readonly ILogger<InMemoryVectorDatabase> _logger;
    private readonly object _gate = new();

    public InMemoryVectorDatabase(ILogger<InMemoryVectorDatabase> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task UpsertVectorsAsync(Guid tenantId, IEnumerable<EmbeddingVector> vectors, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB upsert", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(vectors);

        lock (_gate)
        {
            if (!_storage.TryGetValue(tenantId, out var list))
            {
                list = [];
                _storage[tenantId] = list;
            }

            foreach (var vec in vectors)
            {
                list.RemoveAll(item => item.Vector.ChunkId == vec.ChunkId);
                list.Add(new StoredVector(vec with { TenantId = tenantId }));
            }
        }

        _logger.LogInformation("[InMemory] Upserted vectors for Tenant {TenantId}", tenantId);
        return Task.CompletedTask;
    }

    public Task<List<KnowledgeSearchResult>> SearchVectorsAsync(
        Guid tenantId,
        float[] queryVector,
        int topK = 5,
        double minScore = 0.5,
        Guid? knowledgeBaseId = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB isolation", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(queryVector);

        var tagSet = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        lock (_gate)
        {
            if (!_storage.TryGetValue(tenantId, out var tenantVectors))
                return Task.FromResult(new List<KnowledgeSearchResult>());

            var results = tenantVectors
                .Where(item => !knowledgeBaseId.HasValue || knowledgeBaseId.Value == Guid.Empty || item.Vector.KnowledgeBaseId == knowledgeBaseId.Value)
                .Where(item => tagSet is null || tagSet.Count == 0 || HasAnyTag(item.Vector.Tags, tagSet))
                .Select(item => (item.Vector, Similarity: CosineSimilarity(queryVector, item.Vector.Values)))
                .Where(x => x.Similarity >= minScore)
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .Select(x => new KnowledgeSearchResult(
                    x.Vector.ChunkId,
                    x.Vector.DocumentId,
                    string.Empty,
                    string.Empty,
                    x.Similarity,
                    1.0 - x.Similarity,
                    x.Vector.MetadataJson,
                    []))
                .ToList();

            return Task.FromResult(results);
        }
    }

    public Task DeleteVectorsAsync(Guid tenantId, IEnumerable<Guid> chunkIds, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB delete", nameof(tenantId));
        var idSet = chunkIds.ToHashSet();

        lock (_gate)
        {
            if (_storage.TryGetValue(tenantId, out var tenantVectors))
                tenantVectors.RemoveAll(item => idSet.Contains(item.Vector.ChunkId));
        }

        return Task.CompletedTask;
    }

    public static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (var i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static bool HasAnyTag(IReadOnlyList<string>? tags, HashSet<string> required)
        => tags is not null && tags.Any(required.Contains);

    private sealed record StoredVector(EmbeddingVector Vector);
}

/// <summary>
/// PostgreSQL pgvector-backed vector store with tenant / knowledge-base / tag filters.
/// </summary>
public sealed class PgVectorDatabase : IVectorDatabase
{
    public VectorDbProviderType ProviderType => VectorDbProviderType.PgVector;

    private readonly DbContext _dbContext;
    private readonly KnowledgeOptions _options;
    private readonly ILogger<PgVectorDatabase> _logger;

    public PgVectorDatabase(
        DbContext dbContext,
        IOptions<KnowledgeOptions> options,
        ILogger<PgVectorDatabase> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task UpsertVectorsAsync(Guid tenantId, IEnumerable<EmbeddingVector> vectors, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB upsert", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(vectors);

        var list = vectors.ToList();
        if (list.Count == 0) return;

        foreach (var vector in list)
        {
            if (vector.Values.Length != _options.EmbeddingDimensions)
            {
                throw new InvalidOperationException(
                    $"Embedding dimension mismatch for chunk {vector.ChunkId}: expected {_options.EmbeddingDimensions}, got {vector.Values.Length}.");
            }

            var existing = await _dbContext.Set<KnowledgeEmbedding>()
                .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.ChunkId == vector.ChunkId, cancellationToken);

            var tagsJson = JsonSerializer.Serialize(vector.Tags ?? Array.Empty<string>());

            if (existing is null)
            {
                var entity = new KnowledgeEmbedding(
                    Guid.NewGuid(),
                    vector.ChunkId,
                    vector.DocumentId,
                    vector.KnowledgeBaseId,
                    tenantId,
                    vector.Provider,
                    vector.Model,
                    vector.Values,
                    vector.MetadataJson,
                    tagsJson);
                await _dbContext.Set<KnowledgeEmbedding>().AddAsync(entity, cancellationToken);
            }
            else
            {
                existing.UpdateVector(vector.Values, vector.Provider, vector.Model, vector.MetadataJson, tagsJson);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[PgVector] Upserted {Count} vectors for Tenant {TenantId}", list.Count, tenantId);
    }

    public async Task<List<KnowledgeSearchResult>> SearchVectorsAsync(
        Guid tenantId,
        float[] queryVector,
        int topK = 5,
        double minScore = 0.5,
        Guid? knowledgeBaseId = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB isolation", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(queryVector);

        if (queryVector.Length != _options.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Query embedding dimension mismatch: expected {_options.EmbeddingDimensions}, got {queryVector.Length}.");
        }

        var maxDistance = 1.0 - minScore;
        var tagList = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                      ?? [];

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT e."ChunkId",
                   e."DocumentId",
                   e."MetadataJson",
                   (e."Embedding" <=> @query) AS "Distance"
            FROM "KnowledgeEmbeddings" e
            WHERE e."TenantId" = @tenantId
              AND (@knowledgeBaseId IS NULL OR e."KnowledgeBaseId" = @knowledgeBaseId)
              AND (
                    cardinality(@tags) = 0
                    OR EXISTS (
                        SELECT 1
                        FROM jsonb_array_elements_text(COALESCE(e."TagsJson", '[]')::jsonb) AS t(tag)
                        WHERE lower(t.tag) = ANY(@tags)
                    )
                  )
              AND (e."Embedding" <=> @query) <= @maxDistance
            ORDER BY "Distance"
            LIMIT @topK
            """,
            (NpgsqlConnection)connection);

        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.Add(new NpgsqlParameter("knowledgeBaseId", NpgsqlDbType.Uuid)
        {
            Value = knowledgeBaseId is { } kb && kb != Guid.Empty ? kb : DBNull.Value
        });
        command.Parameters.Add(new NpgsqlParameter("tags", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = tagList.Select(t => t.ToLowerInvariant()).ToArray()
        });
        command.Parameters.AddWithValue("maxDistance", maxDistance);
        command.Parameters.AddWithValue("topK", topK);
        command.Parameters.AddWithValue("query", new Vector(queryVector));

        var rows = new List<(Guid ChunkId, Guid DocumentId, string MetadataJson, double Distance)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.IsDBNull(2) ? "{}" : reader.GetString(2),
                    reader.GetDouble(3)));
            }
        }

        if (rows.Count == 0)
            return [];

        var chunkIds = rows.Select(r => r.ChunkId).ToList();
        var chunks = await _dbContext.Set<KnowledgeChunk>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && chunkIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var documentIds = rows.Select(r => r.DocumentId).Distinct().ToList();
        var documents = await _dbContext.Set<KnowledgeDocument>()
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && documentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        return rows.Select(r =>
        {
            chunks.TryGetValue(r.ChunkId, out var chunk);
            documents.TryGetValue(r.DocumentId, out var doc);
            var score = 1.0 - r.Distance;
            return new KnowledgeSearchResult(
                r.ChunkId,
                r.DocumentId,
                doc?.Title ?? string.Empty,
                chunk?.Content ?? string.Empty,
                score,
                r.Distance,
                r.MetadataJson,
                []);
        }).ToList();
    }

    public async Task DeleteVectorsAsync(Guid tenantId, IEnumerable<Guid> chunkIds, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB delete", nameof(tenantId));
        var idSet = chunkIds.ToHashSet();
        if (idSet.Count == 0) return;

        var entities = await _dbContext.Set<KnowledgeEmbedding>()
            .Where(e => e.TenantId == tenantId && idSet.Contains(e.ChunkId))
            .ToListAsync(cancellationToken);

        if (entities.Count == 0) return;

        _dbContext.Set<KnowledgeEmbedding>().RemoveRange(entities);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Obsolete alias. Prefer <see cref="InMemoryVectorDatabase"/> (tests) or <see cref="PgVectorDatabase"/> (production).
/// </summary>
[Obsolete("Use InMemoryVectorDatabase for tests or PgVectorDatabase for production.")]
public sealed class PgVectorStorage : InMemoryVectorDatabase
{
    public PgVectorStorage(ILogger<InMemoryVectorDatabase> logger) : base(logger)
    {
    }
}

[Obsolete("External vector backends are not implemented. Use PgVectorDatabase.")]
public sealed class QdrantVectorStorage : InMemoryVectorDatabase
{
    public QdrantVectorStorage(ILogger<InMemoryVectorDatabase> logger) : base(logger) { }
}

[Obsolete("External vector backends are not implemented. Use PgVectorDatabase.")]
public sealed class ChromaVectorStorage : InMemoryVectorDatabase
{
    public ChromaVectorStorage(ILogger<InMemoryVectorDatabase> logger) : base(logger) { }
}

[Obsolete("External vector backends are not implemented. Use PgVectorDatabase.")]
public sealed class PineconeVectorStorage : InMemoryVectorDatabase
{
    public PineconeVectorStorage(ILogger<InMemoryVectorDatabase> logger) : base(logger) { }
}
