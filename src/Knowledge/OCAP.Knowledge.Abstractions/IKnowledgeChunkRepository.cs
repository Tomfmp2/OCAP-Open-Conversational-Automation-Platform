using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Abstractions;

public interface IKnowledgeChunkRepository
{
    // Si la organización tiene algo indexado: sin esto cada mensaje pagaría una llamada de embeddings para no encontrar nada.
    Task<bool> AnyForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeChunk>> GetByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddBatchAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default);
}
