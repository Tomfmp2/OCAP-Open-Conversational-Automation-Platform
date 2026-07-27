using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Core.Ports;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly OCAPDbContext _context;

    public ConversationRepository(OCAPDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Where(c => c.UserId == userId && c.Status != ConversationStatus.Closed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Conversations.FindAsync(new object[] { conversation.Id }, cancellationToken);
        if (existing == null)
        {
            await _context.Conversations.AddAsync(conversation, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(conversation);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.AnyAsync(c => c.Id == id, cancellationToken);
    }
}
