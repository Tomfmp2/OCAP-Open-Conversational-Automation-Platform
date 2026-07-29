using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class AiExecutionLogRepository : IAiExecutionLogRepository
{
    private readonly OCAPDbContext _context;

    public AiExecutionLogRepository(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveAsync(AiExecutionLog log, CancellationToken cancellationToken = default)
    {
        await _context.AiExecutionLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
