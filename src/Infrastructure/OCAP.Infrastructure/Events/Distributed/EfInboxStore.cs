using Microsoft.EntityFrameworkCore;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Events.Distributed;

public class EfInboxStore : IInboxStore
{
    private readonly OCAPDbContext _dbContext;

    public EfInboxStore(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> HasBeenProcessedAsync(string messageId, string consumerGroup = "Default", CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .AnyAsync(m => m.MessageId == messageId && m.ConsumerGroup == consumerGroup, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(string messageId, string consumerGroup = "Default", CancellationToken cancellationToken = default)
    {
        var msg = new InboxMessage(Guid.NewGuid(), messageId, consumerGroup);
        _dbContext.InboxMessages.Add(msg);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
