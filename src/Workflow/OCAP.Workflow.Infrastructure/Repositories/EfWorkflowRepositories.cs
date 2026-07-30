using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;

namespace OCAP.Workflow.Infrastructure.Repositories;

public class EfWorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly OCAPDbContext _context;

    public EfWorkflowDefinitionRepository(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowDefinition>()
            .Include(d => d.Steps)
            .Include(d => d.Transitions)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowDefinition>()
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.Set<WorkflowDefinition>().AddAsync(definition, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _context.Set<WorkflowDefinition>().Update(definition);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<WorkflowDefinition>().FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, cancellationToken);
        if (item != null)
        {
            _context.Set<WorkflowDefinition>().Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class EfWorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly OCAPDbContext _context;

    public EfWorkflowExecutionRepository(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WorkflowExecution?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<WorkflowExecution>().Where(e => e.Id == id);
        if (tenantId != Guid.Empty)
            query = query.Where(e => e.TenantId == tenantId);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowExecution>> GetDueDelayedExecutionsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowExecution>()
            .Where(e => e.Status == Domain.Enums.WorkflowStatus.Paused
                        && e.WaitUntilUtc != null
                        && e.WaitUntilUtc <= utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowExecution>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowExecution>()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        await _context.Set<WorkflowExecution>().AddAsync(execution, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        _context.Set<WorkflowExecution>().Update(execution);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddHistoryAsync(WorkflowExecutionHistory history, CancellationToken cancellationToken = default)
    {
        await _context.Set<WorkflowExecutionHistory>().AddAsync(history, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowExecutionHistory>> GetHistoryAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowExecutionHistory>()
            .AsNoTracking()
            .Where(h => h.ExecutionId == executionId)
            .OrderBy(h => h.ExecutedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVariable>> GetVariablesAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowVariable>()
            .AsNoTracking()
            .Where(v => v.ExecutionId == executionId)
            .ToListAsync(cancellationToken);
    }

    public async Task SetVariablesAsync(Guid executionId, IEnumerable<WorkflowVariable> variables, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<WorkflowVariable>()
            .Where(v => v.ExecutionId == executionId)
            .ToListAsync(cancellationToken);

        _context.Set<WorkflowVariable>().RemoveRange(existing);
        await _context.Set<WorkflowVariable>().AddRangeAsync(variables, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
