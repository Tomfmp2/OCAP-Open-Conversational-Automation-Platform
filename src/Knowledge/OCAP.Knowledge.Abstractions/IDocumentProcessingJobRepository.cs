using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Abstractions;

public interface IDocumentProcessingJobRepository
{
    Task<DocumentProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentProcessingJob>> GetPendingJobsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default);
    Task CreateAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default) => AddAsync(job, cancellationToken);
    Task UpdateAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default);
}
