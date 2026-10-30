using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Configuration;
using OCAP.Api.Extensions;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WebChat.Services;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Controllers;

public sealed record WidgetMessageRequest(Guid SessionId, string Message, string? DisplayName);
public sealed record WidgetMessageDto(Guid Id, string From, string Text, DateTime CreatedAt);

/// <summary>
/// API del widget de WebChat que se incrusta en el sitio del cliente. Sin sesión: el canal se identifica con su
/// clave pública y solo atiende a los dominios permitidos del canal. Responde el agente asignado al canal, la
/// conversación aparece en la bandeja y, si una persona la toma, el widget recoge sus respuestas consultando.
/// </summary>
[ApiController]
[Route("api/public/webchat/{publicKey}")]
[AllowAnonymous]
[EnableCors(ApiServiceExtensions.PublicWidgetCorsPolicy)]
public sealed class PublicWebChatController : ControllerBase
{
    public const int MaxMessageLength = 2000;
    private const int MaxDisplayNameLength = 60;

    private readonly IChannelConnectionManager _connections;

    public PublicWebChatController(IChannelConnectionManager connections) => _connections = connections;

    [HttpGet("config")]
    [EnableRateLimiting(RateLimitingSettings.WidgetReadPolicy)]
    public async Task<IActionResult> Config(string publicKey, CancellationToken cancellationToken)
    {
        var connection = await FindAsync(publicKey, cancellationToken);
        if (connection is null) return NotFound();
        if (!OriginAllowed(connection)) return Forbidden();

        return Ok(new { title = connection.ConfigurationMetadata.GetValueOrDefault("WidgetTitle") ?? connection.DisplayName });
    }

    [HttpPost("messages")]
    [EnableRateLimiting(RateLimitingSettings.WidgetMessagesPolicy)]
    public async Task<IActionResult> Send(
        string publicKey,
        [FromBody] WidgetMessageRequest request,
        [FromServices] IConversationResponder responder,
        CancellationToken cancellationToken)
    {
        var message = request.Message?.Trim();
        if (request.SessionId == Guid.Empty || string.IsNullOrEmpty(message) || message.Length > MaxMessageLength)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"El mensaje debe tener entre 1 y {MaxMessageLength} caracteres.");

        var connection = await FindAsync(publicKey, cancellationToken);
        if (connection is null) return NotFound();
        if (!OriginAllowed(connection)) return Forbidden();

        HttpTenantContext.UseTrustedTenant(HttpContext, connection.TenantId);
        var displayName = Clean(request.DisplayName) ?? "Visitante web";
        var reply = await responder.RespondAsync(
            connection.TenantId,
            connection.Id,
            new IncomingChannelMessage { ChannelName = "WebChat", ExternalUserId = request.SessionId.ToString("N"), Message = message },
            displayName,
            cancellationToken);

        // null: una persona del equipo atiende la conversación; su respuesta llega por /messages.
        return Ok(new { reply });
    }

    [HttpGet("sessions/{sessionId:guid}/messages")]
    [EnableRateLimiting(RateLimitingSettings.WidgetReadPolicy)]
    public async Task<IActionResult> Messages(
        string publicKey,
        Guid sessionId,
        [FromQuery] DateTime? after,
        [FromServices] OCAPDbContext db,
        CancellationToken cancellationToken)
    {
        var connection = await FindAsync(publicKey, cancellationToken);
        if (connection is null) return NotFound();
        if (!OriginAllowed(connection)) return Forbidden();

        HttpTenantContext.UseTrustedTenant(HttpContext, connection.TenantId);
        var contact = sessionId.ToString("N");
        var conversationId = await db.Conversations
            .Where(c => c.Channel == "WebChat" && c.ChannelConnectionId == connection.Id && c.ExternalContactId == contact)
            .OrderByDescending(c => c.LastActivityAt)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (conversationId is null) return Ok(Array.Empty<WidgetMessageDto>());

        var since = after ?? DateTime.MinValue;
        var messages = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.CreatedAt > since && m.SenderType != SenderType.System)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(messages.Select(m => new WidgetMessageDto(
            m.Id,
            m.SenderType switch { SenderType.User => "visitor", SenderType.Operator => "operator", _ => "agent" },
            m.Content.Value,
            m.CreatedAt)));
    }

    private async Task<ChannelConnection?> FindAsync(string publicKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicKey) || publicKey.Length > 64) return null;
        var expected = Encoding.UTF8.GetBytes(publicKey);
        var connections = await _connections.GetEnabledConnectionsByProviderAsync("WebChat", cancellationToken);
        return connections.FirstOrDefault(c =>
            c.ConfigurationMetadata.TryGetValue(WebChatRuntimeManager.WidgetPublicKeyMetadata, out var key)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), expected));
    }

    /// <summary>
    /// Sin dominios configurados el widget funciona en cualquier sitio; con ellos, solo en esos. El navegador no deja
    /// falsificar Origin, así que otro sitio no puede gastar la IA del canal desde el navegador de sus visitantes.
    /// </summary>
    private bool OriginAllowed(ChannelConnection connection)
    {
        var configured = connection.ConfigurationMetadata.GetValueOrDefault(WebChatRuntimeManager.AllowedOriginsMetadata);
        if (string.IsNullOrWhiteSpace(configured)) return true;

        var origin = Request.Headers.Origin.FirstOrDefault();
        return origin is not null && configured
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(allowed => string.Equals(allowed.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));
    }

    private ObjectResult Forbidden() =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "Este sitio no está autorizado para usar el chat.");

    private static string? Clean(string? value)
    {
        var text = new string((value ?? string.Empty).Where(c => !char.IsControl(c)).ToArray()).Trim();
        return text.Length == 0 ? null : text[..Math.Min(text.Length, MaxDisplayNameLength)];
    }
}
