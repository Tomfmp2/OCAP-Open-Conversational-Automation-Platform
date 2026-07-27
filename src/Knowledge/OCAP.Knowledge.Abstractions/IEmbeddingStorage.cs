using OCAP.Knowledge.Domain.ValueObjects;

namespace OCAP.Knowledge.Abstractions;

public interface IEmbeddingStorage
{
    Task StoreVectorsAsync(List<EmbeddingVector> vectors, CancellationToken cancellationToken = default);
    Task StoreVectorsAsync(Guid tenantId, IEnumerable<EmbeddingVector> vectors, CancellationToken cancellationToken = default) => StoreVectorsAsync(vectors.ToList(), cancellationToken);
    Task DeleteVectorsByDocumentAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken = default);
}
