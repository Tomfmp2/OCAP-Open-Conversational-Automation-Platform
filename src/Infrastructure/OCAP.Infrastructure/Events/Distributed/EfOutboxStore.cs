using Microsoft.EntityFrameworkCore;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Events.Distributed;

public class EfOutboxStore : IOutboxStore
{
    private readonly OCAPDbContext _dbContext;

    public EfOutboxStore(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.DistributedOutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DistributedOutboxMessages
            .Where(m => m.Status == "Pending")
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var msg = await _dbContext.DistributedOutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (msg != null)
        {
            msg.MarkAsProcessed();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var msg = await _dbContext.DistributedOutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (msg != null)
        {
            msg.MarkAsFailed(error);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
