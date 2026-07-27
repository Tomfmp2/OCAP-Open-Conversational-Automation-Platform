using Microsoft.EntityFrameworkCore;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;

namespace OCAP.Knowledge.Infrastructure.Repositories;

// Implementación de repositorios de Knowledge respaldada por DbContext con filtrado estricto por Tenant.
public class EfKnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly DbContext _context;

    public EfKnowledgeBaseRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<KnowledgeBase>().FirstOrDefaultAsync(kb => kb.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeBase>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Se aplica filtro obligatorio por TenantId para impedir exposición cruzada de información entre organizaciones.
        return await _context.Set<KnowledgeBase>()
            .AsNoTracking()
            .Where(kb => kb.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default)
    {
        await _context.Set<KnowledgeBase>().AddAsync(knowledgeBase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken = default)
    {
        _context.Set<KnowledgeBase>().Update(knowledgeBase);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<KnowledgeBase>().FirstOrDefaultAsync(kb => kb.Id == id && kb.TenantId == tenantId, cancellationToken);
        if (item != null)
        {
            _context.Set<KnowledgeBase>().Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class EfKnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
    private readonly DbContext _context;

    public EfKnowledgeDocumentRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<KnowledgeDocument>().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> GetByKnowledgeBaseAsync(Guid knowledgeBaseId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Se aplica filtro de TenantId y condicionalmente por KnowledgeBaseId
        var query = _context.Set<KnowledgeDocument>()
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId);

        if (knowledgeBaseId != Guid.Empty)
        {
            query = query.Where(d => d.KnowledgeBaseId == knowledgeBaseId);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await _context.Set<KnowledgeDocument>().AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        _context.Set<KnowledgeDocument>().Update(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<KnowledgeDocument>().FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);
        if (item != null)
        {
            _context.Set<KnowledgeDocument>().Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class EfKnowledgeChunkRepository : IKnowledgeChunkRepository
{
    private readonly DbContext _context;

    public EfKnowledgeChunkRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<KnowledgeChunk>()
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId && c.TenantId == tenantId)
            .OrderBy(c => c.Index)
            .ToListAsync(cancellationToken);
    }

    public async Task AddBatchAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _context.Set<KnowledgeChunk>().AddRangeAsync(chunks, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var items = await _context.Set<KnowledgeChunk>()
            .Where(c => c.DocumentId == documentId && c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            _context.Set<KnowledgeChunk>().RemoveRange(items);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class EfDocumentProcessingJobRepository : IDocumentProcessingJobRepository
{
    private readonly DbContext _context;

    public EfDocumentProcessingJobRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DocumentProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DocumentProcessingJob>().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentProcessingJob>> GetPendingJobsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DocumentProcessingJob>()
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default)
    {
        await _context.Set<DocumentProcessingJob>().AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default)
    {
        _context.Set<DocumentProcessingJob>().Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
