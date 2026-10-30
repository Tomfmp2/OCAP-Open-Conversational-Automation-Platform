using Microsoft.EntityFrameworkCore;
using OCAP.Agents.Domain.Entities;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Services;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Services.Memory;

/// <summary>
/// Cada minuto, en segundo plano: resume lo antiguo de las conversaciones largas (el agente recibe resumen y últimos
/// mensajes, con un costo por mensaje estable) y, cuando una conversación lleva <see cref="IdleBeforeExtraction"/> sin
/// actividad, guarda lo que conviene recordar de la persona. Nada de esto ocurre mientras alguien espera respuesta.
/// </summary>
public sealed class ConversationMemoryBackgroundService : BackgroundService
{
    public static readonly TimeSpan IdleBeforeExtraction = TimeSpan.FromMinutes(10);
    /// <summary>Mensajes que el agente recibe tal cual; lo anterior va resumido.</summary>
    public const int RecentWindow = 12;
    /// <summary>Se resume cuando quedan al menos estos mensajes fuera de la ventana: evita una llamada por mensaje.</summary>
    private const int SummarizeBatch = 8;
    private const int BatchPerTick = 20;
    /// <summary>Si el modelo falla tanto tiempo, se deja de reintentar esa conversación.</summary>
    private static readonly TimeSpan GiveUpAfter = TimeSpan.FromDays(1);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConversationMemoryBackgroundService> _logger;

    public ConversationMemoryBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ConversationMemoryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falló el ciclo de memoria de conversaciones.");
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task RunOnceAsync(DateTime now, CancellationToken cancellationToken)
    {
        List<(Guid Id, Guid TenantId)> toSummarize, toExtract, assistantToExtract;
        using (var scope = _scopeFactory.CreateScope())
        {
            // Sin organización en el scope: el descubrimiento ve todas (bypass); cada conversación se procesa en la suya.
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            var idleSince = now - IdleBeforeExtraction;

            toExtract = (await db.Conversations.IgnoreQueryFilters()
                    .Where(c => c.Channel != null && c.LastActivityAt < idleSince && (c.MemoryExtractedAt == null || c.MemoryExtractedAt < c.LastActivityAt))
                    .OrderBy(c => c.LastActivityAt).Take(BatchPerTick)
                    .Select(c => new { c.Id, c.TenantId }).ToListAsync(cancellationToken))
                .Select(c => (c.Id, c.TenantId)).ToList();

            var recent = now.AddDays(-1);
            toSummarize = (await db.Conversations.IgnoreQueryFilters()
                    .Where(c => c.Channel != null && c.LastActivityAt > recent)
                    .Where(c => db.Messages.IgnoreQueryFilters().Count(m => m.ConversationId == c.Id
                        && (c.SummarizedUntil == null || m.CreatedAt > c.SummarizedUntil)) >= RecentWindow + SummarizeBatch)
                    .Take(BatchPerTick)
                    .Select(c => new { c.Id, c.TenantId }).ToListAsync(cancellationToken))
                .Select(c => (c.Id, c.TenantId)).ToList();

            assistantToExtract = (await db.Set<AssistantConversation>().IgnoreQueryFilters()
                    .Where(c => !c.HasUntrustedContent && c.UpdatedAt < idleSince && (c.MemoryExtractedAt == null || c.MemoryExtractedAt < c.UpdatedAt))
                    .OrderBy(c => c.UpdatedAt).Take(BatchPerTick)
                    .Select(c => new { c.Id, c.TenantId }).ToListAsync(cancellationToken))
                .Select(c => (c.Id, c.TenantId)).ToList();
        }

        foreach (var (id, tenantId) in toSummarize) await InTenantAsync(tenantId, (sp, ct) => SummarizeAsync(sp, id, ct), cancellationToken);
        foreach (var (id, tenantId) in toExtract) await InTenantAsync(tenantId, (sp, ct) => ExtractChannelAsync(sp, id, now, ct), cancellationToken);
        foreach (var (id, tenantId) in assistantToExtract) await InTenantAsync(tenantId, (sp, ct) => ExtractAssistantAsync(sp, id, now, ct), cancellationToken);
    }

    private async Task InTenantAsync(Guid tenantId, Func<IServiceProvider, CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantScope>().Use(tenantId);
        try
        {
            await work(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo procesar la memoria de una conversación de la organización {TenantId}.", tenantId);
        }
    }

    private static async Task SummarizeAsync(IServiceProvider services, Guid conversationId, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<OCAPDbContext>();
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null) return;

        var pending = await db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderType != SenderType.System
                        && (conversation.SummarizedUntil == null || m.CreatedAt > conversation.SummarizedUntil))
            .OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken);
        var older = pending.Take(Math.Max(0, pending.Count - RecentWindow)).ToList();
        if (older.Count < SummarizeBatch) return;

        var summary = await services.GetRequiredService<IMemoryModel>().SummarizeAsync(conversation.TenantId, conversation.Summary,
            older.Select(m => new MemoryTurn(m.SenderType == SenderType.User, m.Content.Value)).ToList(), cancellationToken);
        if (summary is null) return;

        conversation.UpdateSummary(summary.Length > 4000 ? summary[..4000] : summary, older[^1].CreatedAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task ExtractChannelAsync(IServiceProvider services, Guid conversationId, DateTime now, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<OCAPDbContext>();
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null) return;

        var lifetime = await LifetimeAsync(db, conversation.TenantId, cancellationToken);
        if (lifetime is null || !await AgentRemembersAsync(services, db, conversation, cancellationToken))
        {
            conversation.MarkMemoryExtracted(conversation.LastActivityAt);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var turns = (await db.Messages
                .Where(m => m.ConversationId == conversationId && m.SenderType != SenderType.System
                            && (conversation.MemoryExtractedAt == null || m.CreatedAt > conversation.MemoryExtractedAt))
                .OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken))
            .Select(m => new MemoryTurn(m.SenderType == SenderType.User, m.Content.Value)).ToList();

        await ExtractAsync(services, conversation.TenantId, MemoryOwnerKind.Contact, conversation.UserId, conversation.Id, turns,
            lifetime.Value, now, conversation.LastActivityAt, () => conversation.MarkMemoryExtracted(conversation.LastActivityAt), cancellationToken);
    }

    private static async Task ExtractAssistantAsync(IServiceProvider services, Guid conversationId, DateTime now, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<OCAPDbContext>();
        var conversation = await db.Set<AssistantConversation>().FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null) return;

        var lifetime = await LifetimeAsync(db, conversation.TenantId, cancellationToken);
        if (lifetime is null)
        {
            conversation.MarkMemoryExtracted(conversation.UpdatedAt);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var turns = (await db.Set<AssistantMessage>()
                .Where(m => m.ConversationId == conversationId && (conversation.MemoryExtractedAt == null || m.CreatedAt > conversation.MemoryExtractedAt))
                .OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken))
            .Select(m => new MemoryTurn(m.Role == AssistantRoles.User, m.Content)).ToList();

        await ExtractAsync(services, conversation.TenantId, MemoryOwnerKind.PanelUser, conversation.UserId, null, turns,
            lifetime.Value, now, conversation.UpdatedAt, () => conversation.MarkMemoryExtracted(conversation.UpdatedAt), cancellationToken);
    }

    private static async Task ExtractAsync(IServiceProvider services, Guid tenantId, MemoryOwnerKind ownerKind, Guid ownerId,
        Guid? conversationId, IReadOnlyList<MemoryTurn> turns, TimeSpan lifetime, DateTime now, DateTime lastActivity,
        Action markExtracted, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<OCAPDbContext>();
        // Un saludo suelto no deja nada que recordar y no merece una llamada al modelo.
        if (turns.Count(t => t.FromPerson) >= 2)
        {
            var store = services.GetRequiredService<ContactMemoryStore>();
            var known = await store.RecallAsync(ownerKind, ownerId, now, cancellationToken);
            var extraction = await services.GetRequiredService<IMemoryModel>().ExtractAsync(tenantId, known.Facts, turns, cancellationToken);
            if (extraction is null && now - lastActivity < GiveUpAfter) return; // se reintenta en el próximo ciclo
            if (extraction is not null)
                await store.ApplyAsync(tenantId, ownerKind, ownerId, conversationId, extraction.Facts, extraction.Episode, lifetime, now, cancellationToken);
        }

        markExtracted();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Vigencia de los recuerdos de la organización; null si desactivó la memoria.</summary>
    private static async Task<TimeSpan?> LifetimeAsync(OCAPDbContext db, Guid tenantId, CancellationToken cancellationToken) =>
        await TenantSettingsSections.ReadAsync<OrganizationPreferences>(db, tenantId, OrganizationPreferences.Section, cancellationToken) is { } preferences
            ? preferences.MemoryLifetime
            : TimeSpan.FromDays(OrganizationPreferences.DefaultMemoryDays);

    /// <summary>El agente del canal puede tener la memoria apagada («Recordar clientes» en su configuración).</summary>
    private static async Task<bool> AgentRemembersAsync(IServiceProvider services, OCAPDbContext db, Conversation conversation, CancellationToken cancellationToken)
    {
        if (conversation.ChannelConnectionId is not { } connectionId) return true;
        var connection = await db.ChannelConnections.FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);
        if (connection?.ConfigurationMetadata.GetValueOrDefault(ChannelConversationService.AgentIdMetadataKey) is not { } raw
            || !Guid.TryParse(raw, out var agentId))
            return true;
        var agent = await services.GetRequiredService<OCAP.Agents.Abstractions.Ports.IAgentRepository>().GetByIdAsync(agentId, cancellationToken);
        return agent?.Configuration.RemembersContacts ?? true;
    }
}
