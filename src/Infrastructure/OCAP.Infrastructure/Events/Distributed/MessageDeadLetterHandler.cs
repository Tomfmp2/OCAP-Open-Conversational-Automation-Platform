using Microsoft.EntityFrameworkCore;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Events.Distributed;

public class MessageDeadLetterHandler : IMessageDeadLetterHandler
{
    private readonly OCAPDbContext _dbContext;

    public MessageDeadLetterHandler(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task HandleDeadLetterAsync(Guid tenantId, string eventType, string payloadJson, string reason, int retryCount, CancellationToken cancellationToken = default)
    {
        var msg = new DeadLetterMessage(Guid.NewGuid(), tenantId, eventType, payloadJson, reason, retryCount);
        _dbContext.DeadLetterMessages.Add(msg);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DeadLetterMessage>> GetDeadLettersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages
            .Where(m => m.TenantId == tenantId && !m.Replayed)
            .OrderByDescending(m => m.FailedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ReplayDeadLetterAsync(Guid deadLetterId, CancellationToken cancellationToken = default)
    {
        var msg = await _dbContext.DeadLetterMessages.FirstOrDefaultAsync(m => m.Id == deadLetterId, cancellationToken);
        if (msg == null) return false;

        msg.MarkAsReplayed();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
