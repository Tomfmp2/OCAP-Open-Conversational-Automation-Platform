using OCAP.Knowledge.Domain.Enums;
using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IVectorDatabase
{
    VectorDbProviderType ProviderType { get; }

    Task UpsertVectorsAsync(Guid tenantId, IEnumerable<EmbeddingVector> vectors, CancellationToken cancellationToken = default);

    Task UpsertVectorsAsync(Guid tenantId, List<EmbeddingVector> vectors, CancellationToken cancellationToken = default)
        => UpsertVectorsAsync(tenantId, (IEnumerable<EmbeddingVector>)vectors, cancellationToken);

    Task<List<KnowledgeSearchResult>> SearchVectorsAsync(
        Guid tenantId,
        float[] queryVector,
        int topK = 5,
        double minScore = 0.5,
        Guid? knowledgeBaseId = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    Task DeleteVectorsAsync(Guid tenantId, IEnumerable<Guid> chunkIds, CancellationToken cancellationToken = default);
}
