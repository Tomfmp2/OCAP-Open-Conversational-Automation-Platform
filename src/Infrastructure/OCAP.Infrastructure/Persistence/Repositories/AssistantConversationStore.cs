using Microsoft.EntityFrameworkCore;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

// La organización la filtran el filtro global y RLS; el dueño, cada consulta.
public class AssistantConversationStore : IAssistantConversationStore
{
    private readonly OCAPDbContext _context;

    public AssistantConversationStore(OCAPDbContext context) => _context = context;

    public async Task<IReadOnlyList<AssistantConversation>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Set<AssistantConversation>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<AssistantConversation?> FindAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default) =>
        _context.Set<AssistantConversation>().FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<AssistantMessage>> GetMessagesAsync(Guid conversationId, int? last = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<AssistantMessage>().AsNoTracking().Where(m => m.ConversationId == conversationId);
        if (last is { } count)
        {
            var recent = await query.OrderByDescending(m => m.CreatedAt).Take(count).ToListAsync(cancellationToken);
            recent.Reverse();
            return recent;
        }
        return await query.OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AssistantConversation conversation, CancellationToken cancellationToken = default)
    {
        _context.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessagesAsync(AssistantConversation conversation, IEnumerable<AssistantMessage> messages, CancellationToken cancellationToken = default)
    {
        _context.AddRange(messages);
        conversation.Touch();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(AssistantConversation conversation, CancellationToken cancellationToken = default)
    {
        // Los mensajes se borran en cascada.
        _context.Remove(conversation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
