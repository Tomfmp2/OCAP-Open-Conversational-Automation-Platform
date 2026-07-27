using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Abstractions;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> GetByKnowledgeBaseAsync(Guid knowledgeBaseId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
    Task CreateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default) => AddAsync(document, cancellationToken);
    Task UpdateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}
