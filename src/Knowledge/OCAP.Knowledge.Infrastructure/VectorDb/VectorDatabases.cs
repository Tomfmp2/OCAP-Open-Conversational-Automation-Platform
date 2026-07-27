using Microsoft.Extensions.Logging;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Infrastructure.VectorDb;

public abstract class BaseVectorDatabase : IVectorDatabase
{
    public abstract VectorDbProviderType ProviderType { get; }
    protected readonly Dictionary<Guid, List<(EmbeddingVector Vector, Guid TenantId)>> Storage = new();
    protected readonly ILogger Logger;

    protected BaseVectorDatabase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task UpsertVectorsAsync(Guid tenantId, IEnumerable<EmbeddingVector> vectors, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB upsert", nameof(tenantId));

        lock (Storage)
        {
            if (!Storage.ContainsKey(tenantId))
            {
                Storage[tenantId] = new List<(EmbeddingVector, Guid)>();
            }

            foreach (var vec in vectors)
            {
                Storage[tenantId].RemoveAll(item => item.Vector.ChunkId == vec.ChunkId);
                Storage[tenantId].Add((vec, tenantId));
            }
        }

        Logger.LogInformation("[{Provider}] Upserted {Count} vectors for Tenant {TenantId}", ProviderType, vectors.Count(), tenantId);
        return Task.CompletedTask;
    }

    public Task<List<KnowledgeSearchResult>> SearchVectorsAsync(Guid tenantId, float[] queryVector, int topK = 5, double minScore = 0.5, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB isolation", nameof(tenantId));

        var results = new List<KnowledgeSearchResult>();

        lock (Storage)
        {
            if (!Storage.TryGetValue(tenantId, out var tenantVectors))
            {
                return Task.FromResult(results);
            }

            var scoredList = new List<(EmbeddingVector Vector, double Similarity)>();

            foreach (var item in tenantVectors)
            {
                double sim = CosineSimilarity(queryVector, item.Vector.Values);
                if (sim >= minScore)
                {
                    scoredList.Add((item.Vector, sim));
                }
            }

            results = scoredList
                .OrderByDescending(s => s.Similarity)
                .Take(topK)
                .Select(s => new KnowledgeSearchResult(
                    s.Vector.ChunkId,
                    Guid.NewGuid(),
                    "Document Metadata Result",
                    $"[Content from Chunk {s.Vector.ChunkId}]",
                    s.Similarity,
                    1.0 - s.Similarity,
                    "{}",
                    new List<string>()
                ))
                .ToList();
        }

        return Task.FromResult(results);
    }

    public Task DeleteVectorsAsync(Guid tenantId, IEnumerable<Guid> chunkIds, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required for Vector DB delete", nameof(tenantId));

        lock (Storage)
        {
            if (Storage.TryGetValue(tenantId, out var tenantVectors))
            {
                var idSet = chunkIds.ToHashSet();
                tenantVectors.RemoveAll(item => idSet.Contains(item.Vector.ChunkId));
            }
        }

        return Task.CompletedTask;
    }

    protected static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

public class PgVectorStorage : BaseVectorDatabase
{
    public override VectorDbProviderType ProviderType => VectorDbProviderType.PgVector;
    public PgVectorStorage(ILogger<PgVectorStorage> logger) : base(logger) { }
}

public class QdrantVectorStorage : BaseVectorDatabase
{
    public override VectorDbProviderType ProviderType => VectorDbProviderType.Qdrant;
    public QdrantVectorStorage(ILogger<QdrantVectorStorage> logger) : base(logger) { }
}

public class ChromaVectorStorage : BaseVectorDatabase
{
    public override VectorDbProviderType ProviderType => VectorDbProviderType.ChromaDB;
    public ChromaVectorStorage(ILogger<ChromaVectorStorage> logger) : base(logger) { }
}

public class PineconeVectorStorage : BaseVectorDatabase
{
    public override VectorDbProviderType ProviderType => VectorDbProviderType.Pinecone;
    public PineconeVectorStorage(ILogger<PineconeVectorStorage> logger) : base(logger) { }
}
