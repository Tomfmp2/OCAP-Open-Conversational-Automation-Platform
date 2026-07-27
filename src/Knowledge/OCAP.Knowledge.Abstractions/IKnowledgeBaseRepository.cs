using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Abstractions;

public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeBase>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default);
    Task CreateAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default) => AddAsync(knowledgeBase, cancellationToken);
    Task UpdateAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}
