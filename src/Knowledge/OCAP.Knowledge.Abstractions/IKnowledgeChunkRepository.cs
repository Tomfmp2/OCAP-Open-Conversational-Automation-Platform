using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Abstractions;

public interface IKnowledgeChunkRepository
{
    Task<IReadOnlyList<KnowledgeChunk>> GetByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddBatchAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default);
}
