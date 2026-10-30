using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Queries;

public sealed record InboxConversationDto(
    Guid Id,
    string Channel,
    string ContactName,
    string? ExternalContactId,
    string Status,
    bool HandledByHuman,
    bool CanReply,
    DateTime LastActivityAt,
    string? LastMessage,
    string? LastSender);

public sealed record InboxMessageDto(Guid Id, string Sender, string Text, DateTime CreatedAt);

/// <summary>
/// Lecturas de la bandeja: conversaciones que llegan por canales externos, con el contacto y su último mensaje.
/// Los filtros globales de EF las acotan a la organización de la sesión.
/// </summary>
public sealed class ConversationInboxQueries
{
    private const int MaxConversations = 200;

    private readonly OCAPDbContext _db;

    public ConversationInboxQueries(OCAPDbContext db) => _db = db;

    /// <summary>Conversaciones de canales, de la más reciente; con <c>onlyHuman</c>, solo las que atiende una persona.</summary>
    public async Task<IReadOnlyList<InboxConversationDto>> ListAsync(string? channel, bool onlyHuman, CancellationToken cancellationToken)
    {
        var query = _db.Conversations.AsNoTracking().Where(c => c.Channel != null);
        if (!string.IsNullOrWhiteSpace(channel)) query = query.Where(c => c.Channel == channel);
        if (onlyHuman) query = query.Where(c => c.Status == ConversationStatus.WaitingHuman);

        var rows = await query
            .OrderByDescending(c => c.LastActivityAt)
            .Take(MaxConversations)
            .Select(c => new
            {
                Conversation = c,
                ContactName = _db.Users.Where(u => u.Id == c.UserId).Select(u => u.DisplayName).FirstOrDefault(),
                Last = _db.Messages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new { m.Content, m.SenderType })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new InboxConversationDto(
                r.Conversation.Id,
                r.Conversation.Channel!,
                r.ContactName ?? r.Conversation.ExternalContactId ?? "Contacto",
                r.Conversation.ExternalContactId,
                r.Conversation.Status.ToString(),
                r.Conversation.IsHandledByHuman,
                Services.ChannelReplySender.CanReply(r.Conversation),
                r.Conversation.LastActivityAt,
                r.Last?.Content.Value,
                r.Last?.SenderType.ToString()))
            .ToList();
    }

    public async Task<IReadOnlyList<InboxMessageDto>?> MessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        // Solo conversaciones de canales: el chat interno del panel no pertenece a la bandeja.
        if (!await _db.Conversations.AnyAsync(c => c.Id == conversationId && c.Channel != null, cancellationToken)) return null;

        var messages = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
        return messages.Select(m => new InboxMessageDto(m.Id, m.SenderType.ToString(), m.Content.Value, m.CreatedAt)).ToList();
    }
}
