using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Services.Memory;

public sealed record ExtractedFact(string Key, string Text);

/// <summary>Lo que el agente recibe de la memoria de una persona, ya recortado al presupuesto del prompt.</summary>
public sealed record MemoryRecall(IReadOnlyList<string> Facts, IReadOnlyList<string> Episodes)
{
    public static readonly MemoryRecall Empty = new([], []);
    public bool IsEmpty => Facts.Count == 0 && Episodes.Count == 0;
}

/// <summary>Recuerdos por persona (contacto de un canal o usuario del panel) dentro de la organización activa.</summary>
public sealed class ContactMemoryStore
{
    /// <summary>Presupuesto del prompt: los hechos más recientes y los últimos episodios bastan para dar continuidad.</summary>
    public const int RecallFacts = 15;
    public const int RecallEpisodes = 3;

    private readonly OCAPDbContext _db;

    public ContactMemoryStore(OCAPDbContext db) => _db = db;

    public async Task<MemoryRecall> RecallAsync(MemoryOwnerKind ownerKind, Guid ownerId, DateTime now, CancellationToken cancellationToken)
    {
        var active = Active(ownerKind, ownerId, now);
        var facts = await active.Where(m => m.Kind == MemoryKind.Fact)
            .OrderByDescending(m => m.ConfirmedAt).Take(RecallFacts).Select(m => m.Text).ToListAsync(cancellationToken);
        var episodes = await active.Where(m => m.Kind == MemoryKind.Episode)
            .OrderByDescending(m => m.CreatedAt).Take(RecallEpisodes).Select(m => m.Text).ToListAsync(cancellationToken);
        episodes.Reverse();
        return new MemoryRecall(facts, episodes);
    }

    public Task<List<ContactMemory>> ListAsync(MemoryOwnerKind ownerKind, Guid ownerId, DateTime now, CancellationToken cancellationToken) =>
        Active(ownerKind, ownerId, now).OrderBy(m => m.Kind).ThenByDescending(m => m.ConfirmedAt).ToListAsync(cancellationToken);

    /// <summary>
    /// Guarda lo extraído de una conversación. Un hecho con la misma clave y el mismo texto renueva su vigencia; con otro
    /// texto, el anterior queda reemplazado (no se borra).
    /// </summary>
    public async Task ApplyAsync(Guid tenantId, MemoryOwnerKind ownerKind, Guid ownerId, Guid? conversationId,
        IReadOnlyList<ExtractedFact> facts, string? episode, TimeSpan lifetime, DateTime now, CancellationToken cancellationToken)
    {
        var current = await Active(ownerKind, ownerId, now).Where(m => m.Kind == MemoryKind.Fact).ToListAsync(cancellationToken);
        foreach (var fact in facts)
        {
            var key = ContactMemory.NormalizeKey(fact.Key);
            if (key is null || string.IsNullOrWhiteSpace(fact.Text)) continue;
            var existing = current.FirstOrDefault(m => m.Key == key);
            if (existing is not null && string.Equals(existing.Text, fact.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                existing.Confirm(now, lifetime);
                continue;
            }
            existing?.Supersede(now);
            var added = new ContactMemory(tenantId, ownerKind, ownerId, MemoryKind.Fact, key, fact.Text, conversationId, now, lifetime);
            _db.ContactMemories.Add(added);
            current.Add(added);
        }

        if (!string.IsNullOrWhiteSpace(episode))
            _db.ContactMemories.Add(new ContactMemory(tenantId, ownerKind, ownerId, MemoryKind.Episode, null, episode, conversationId, now, lifetime));

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EditAsync(MemoryOwnerKind ownerKind, Guid ownerId, Guid memoryId, string text, DateTime now, CancellationToken cancellationToken)
    {
        var memory = await Active(ownerKind, ownerId, now).FirstOrDefaultAsync(m => m.Id == memoryId, cancellationToken);
        if (memory is null) return false;
        memory.Edit(text, now);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Borra un recuerdo, o todos (con su historial reemplazado) si no se indica cuál: «olvídame».</summary>
    public async Task<int> ForgetAsync(MemoryOwnerKind ownerKind, Guid ownerId, Guid? memoryId, CancellationToken cancellationToken)
    {
        var query = _db.ContactMemories.Where(m => m.OwnerKind == ownerKind && m.OwnerId == ownerId);
        if (memoryId is { } id) query = query.Where(m => m.Id == id);
        var rows = await query.ToListAsync(cancellationToken);
        _db.ContactMemories.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private IQueryable<ContactMemory> Active(MemoryOwnerKind ownerKind, Guid ownerId, DateTime now) =>
        _db.ContactMemories.Where(m => m.OwnerKind == ownerKind && m.OwnerId == ownerId && m.SupersededAt == null && m.ExpiresAt > now);
}
