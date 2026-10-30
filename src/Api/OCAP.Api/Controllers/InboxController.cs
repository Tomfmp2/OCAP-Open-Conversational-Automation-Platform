using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Queries;
using OCAP.Api.Services;
using OCAP.Application.UseCases;
using OCAP.Core.Entities;
using OCAP.Core.Ports;

namespace OCAP.Api.Controllers;

public sealed record InboxReplyRequest(string Text);

public sealed record EditMemoryRequest(string Text);

/// <summary>Un recuerdo tal como lo ve el panel.</summary>
public sealed record MemoryItemDto(Guid Id, string Kind, string? Key, string Text, DateTime ConfirmedAt, DateTime ExpiresAt)
{
    public static MemoryItemDto From(OCAP.Core.Entities.ContactMemory m) => new(m.Id, m.Kind.ToString(), m.Key, m.Text, m.ConfirmedAt, m.ExpiresAt);
}

/// <summary>
/// Bandeja de conversaciones de los canales: el equipo ve lo que escriben los contactos, toma una conversación
/// (el agente deja de responder), contesta como persona y la devuelve al agente.
/// </summary>
[ApiController]
[Route("api/inbox")]
[Authorize(Policy = OcapPermissions.ConversationRead)]
public sealed class InboxController : ControllerBase
{
    private readonly ConversationInboxQueries _queries;
    private readonly IConversationRepository _conversations;

    public InboxController(ConversationInboxQueries queries, IConversationRepository conversations)
    {
        _queries = queries;
        _conversations = conversations;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InboxConversationDto>>> List(
        [FromQuery] string? channel,
        [FromQuery] bool onlyHuman,
        CancellationToken cancellationToken) =>
        Ok(await _queries.ListAsync(channel, onlyHuman, cancellationToken));

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<InboxMessageDto>>> Messages(Guid id, CancellationToken cancellationToken)
    {
        var messages = await _queries.MessagesAsync(id, cancellationToken);
        return messages is null ? NotFound() : Ok(messages);
    }

    /// <summary>Una persona toma la conversación: el agente deja de responder.</summary>
    [HttpPost("{id:guid}/takeover")]
    [Authorize(Policy = OcapPermissions.ConversationWrite)]
    public Task<IActionResult> TakeOver(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, c => c.TakeOver(), cancellationToken);

    /// <summary>Devuelve la conversación al agente.</summary>
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = OcapPermissions.ConversationWrite)]
    public Task<IActionResult> Release(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, c => c.ReturnToAgent(), cancellationToken);

    /// <summary>
    /// Responde como persona por el canal del contacto. Responder toma la conversación, para que el agente no
    /// conteste encima del equipo.
    /// </summary>
    [HttpPost("{id:guid}/reply")]
    [Authorize(Policy = OcapPermissions.ConversationWrite)]
    public async Task<IActionResult> Reply(
        Guid id,
        [FromBody] InboxReplyRequest request,
        [FromServices] ChannelReplySender sender,
        [FromServices] ReceiveMessageUseCase messages,
        CancellationToken cancellationToken)
    {
        var text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return Problem(statusCode: StatusCodes.Status400BadRequest, title: "El mensaje está vacío.");
        if (text.Length > ReceiveMessageUseCase.MaxMessageLength)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"El mensaje supera {ReceiveMessageUseCase.MaxMessageLength} caracteres.");

        var conversation = await FindChannelConversationAsync(id, cancellationToken);
        if (conversation is null) return NotFound();
        if (!ChannelReplySender.CanReply(conversation))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Esta conversación no tiene un canal al que se pueda responder.");

        if (!await sender.SendAsync(conversation, text, cancellationToken))
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "El canal no aceptó el mensaje. Revisa que la conexión siga activa.");

        conversation.TakeOver();
        conversation.UpdateActivity();
        await _conversations.SaveAsync(conversation, cancellationToken);
        await messages.RecordReplyAsync(conversation.Id, text, SenderType.Operator, cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> ChangeAsync(Guid id, Action<Conversation> change, CancellationToken cancellationToken)
    {
        var conversation = await FindChannelConversationAsync(id, cancellationToken);
        if (conversation is null) return NotFound();
        change(conversation);
        await _conversations.SaveAsync(conversation, cancellationToken);
        return NoContent();
    }

    /// <summary>La bandeja solo opera sobre conversaciones de canales, nunca sobre el chat interno del panel.</summary>
    /// <summary>Lo que los agentes recuerdan del contacto de esta conversación.</summary>
    [HttpGet("{id:guid}/memory")]
    public async Task<IActionResult> GetMemory(Guid id, [FromServices] OCAP.Api.Services.Memory.ContactMemoryStore memory, CancellationToken cancellationToken)
    {
        var conversation = await FindChannelConversationAsync(id, cancellationToken);
        if (conversation is null) return NotFound();
        var items = await memory.ListAsync(OCAP.Core.Entities.MemoryOwnerKind.Contact, conversation.UserId, DateTime.UtcNow, cancellationToken);
        return Ok(items.Select(MemoryItemDto.From));
    }

    [HttpPut("{id:guid}/memory/{memoryId:guid}")]
    [Authorize(Policy = OcapPermissions.ConversationWrite)]
    public async Task<IActionResult> EditMemory(Guid id, Guid memoryId, [FromBody] EditMemoryRequest request,
        [FromServices] OCAP.Api.Services.Memory.ContactMemoryStore memory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > OCAP.Core.Entities.ContactMemory.MaxTextLength)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Escribe el recuerdo (hasta 500 caracteres).");
        var conversation = await FindChannelConversationAsync(id, cancellationToken);
        if (conversation is null) return NotFound();
        return await memory.EditAsync(OCAP.Core.Entities.MemoryOwnerKind.Contact, conversation.UserId, memoryId, request.Text, DateTime.UtcNow, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    /// <summary>Borra un recuerdo o, sin <paramref name="memoryId"/>, todo lo que se recuerda del contacto.</summary>
    [HttpDelete("{id:guid}/memory/{memoryId:guid?}")]
    [Authorize(Policy = OcapPermissions.ConversationWrite)]
    public async Task<IActionResult> ForgetMemory(Guid id, Guid? memoryId,
        [FromServices] OCAP.Api.Services.Memory.ContactMemoryStore memory, CancellationToken cancellationToken)
    {
        var conversation = await FindChannelConversationAsync(id, cancellationToken);
        if (conversation is null) return NotFound();
        await memory.ForgetAsync(OCAP.Core.Entities.MemoryOwnerKind.Contact, conversation.UserId, memoryId, cancellationToken);
        return NoContent();
    }

    private async Task<Conversation?> FindChannelConversationAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(id, cancellationToken);
        return conversation is { Channel: not null } ? conversation : null;
    }
}
