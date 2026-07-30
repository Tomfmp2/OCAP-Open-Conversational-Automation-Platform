using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Events.Distributed;

public class MessageDeadLetterHandler : IMessageDeadLetterHandler
{
    private readonly OCAPDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public MessageDeadLetterHandler(OCAPDbContext dbContext, IServiceProvider serviceProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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

        var outbox = _serviceProvider.GetRequiredService<IOutboxStore>();
        await outbox.SaveAsync(
            new OutboxMessage(Guid.NewGuid(), msg.TenantId, msg.EventType, msg.OriginalPayloadJson),
            cancellationToken);

        msg.MarkAsReplayed();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
