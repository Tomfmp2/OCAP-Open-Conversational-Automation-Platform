using System.Collections.Concurrent;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Application.Services;

public sealed class InMemoryConversationContextRepository : IConversationContextRepository
{
    private readonly ConcurrentDictionary<Guid, ConversationContext> _store = new();

    public Task<ConversationContext?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(conversationId, out var ctx);
        return Task.FromResult(ctx);
    }

    public Task SaveAsync(ConversationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _store[context.ConversationId] = context;
        return Task.CompletedTask;
    }
}
