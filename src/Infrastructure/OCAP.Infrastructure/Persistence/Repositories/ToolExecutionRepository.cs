using OCAP.Core.Entities;
using OCAP.Core.Ports;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class ToolExecutionRepository : IToolExecutionRepository
{
    private readonly OCAPDbContext _context;

    public ToolExecutionRepository(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveAsync(ToolExecution toolExecution, CancellationToken cancellationToken = default)
    {
        await _context.ToolExecutions.AddAsync(toolExecution, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
