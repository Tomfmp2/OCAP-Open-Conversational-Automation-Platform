using Microsoft.EntityFrameworkCore;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class AiConversationMemoryRepository : IAiConversationMemoryRepository
{
    private readonly OCAPDbContext _context;

    public AiConversationMemoryRepository(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveAsync(AiConversationMemory memory, CancellationToken cancellationToken = default)
    {
        await _context.AiConversationMemories.AddAsync(memory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AiConversationMemory>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.AiConversationMemories
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
