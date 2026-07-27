using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Infrastructure.Repositories;

public class InMemoryKnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly List<KnowledgeBase> _list = new();

    public Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            return Task.FromResult(_list.FirstOrDefault(kb => kb.Id == id));
        }
    }

    public Task<IReadOnlyList<KnowledgeBase>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            IReadOnlyList<KnowledgeBase> result = _list.Where(kb => kb.TenantId == tenantId).ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.RemoveAll(kb => kb.Id == knowledgeBase.Id);
            _list.Add(knowledgeBase);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default)
    {
        return AddAsync(knowledgeBase, cancellationToken);
    }

    public Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.RemoveAll(kb => kb.Id == id && kb.TenantId == tenantId);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryKnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
    private readonly List<KnowledgeDocument> _list = new();

    public Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            return Task.FromResult(_list.FirstOrDefault(d => d.Id == id));
        }
    }

    public Task<IReadOnlyList<KnowledgeDocument>> GetByKnowledgeBaseAsync(Guid knowledgeBaseId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            IReadOnlyList<KnowledgeDocument> result = _list
                .Where(d => d.TenantId == tenantId && (knowledgeBaseId == Guid.Empty || d.KnowledgeBaseId == knowledgeBaseId))
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.RemoveAll(d => d.Id == document.Id);
            _list.Add(document);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        return AddAsync(document, cancellationToken);
    }

    public Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.RemoveAll(d => d.Id == id && d.TenantId == tenantId);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryKnowledgeChunkRepository : IKnowledgeChunkRepository
{
    private readonly List<KnowledgeChunk> _list = new();

    public Task<IReadOnlyList<KnowledgeChunk>> GetByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            IReadOnlyList<KnowledgeChunk> result = _list.Where(c => c.DocumentId == documentId && c.TenantId == tenantId).ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddBatchAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.AddRange(chunks);
        }
        return Task.CompletedTask;
    }

    public Task DeleteByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.RemoveAll(c => c.DocumentId == documentId && c.TenantId == tenantId);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryDocumentProcessingJobRepository : IDocumentProcessingJobRepository
{
    private readonly List<DocumentProcessingJob> _list = new();

    public Task<DocumentProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            return Task.FromResult(_list.FirstOrDefault(j => j.Id == id));
        }
    }

    public Task<IReadOnlyList<DocumentProcessingJob>> GetPendingJobsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            IReadOnlyList<DocumentProcessingJob> result = _list.Where(j => j.TenantId == tenantId).ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default)
    {
        lock (_list)
        {
            _list.RemoveAll(j => j.Id == job.Id);
            _list.Add(job);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default)
    {
        return AddAsync(job, cancellationToken);
    }
}
