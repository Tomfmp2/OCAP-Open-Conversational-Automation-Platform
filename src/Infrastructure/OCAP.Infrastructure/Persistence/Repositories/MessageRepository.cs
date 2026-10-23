using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Core.Ports;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly OCAPDbContext _context;

    public MessageRepository(OCAPDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetRecentAsync(Guid conversationId, int take, CancellationToken cancellationToken = default)
    {
        var recent = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        recent.Reverse();
        return recent;
    }
}
