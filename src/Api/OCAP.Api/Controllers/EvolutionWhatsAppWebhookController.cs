using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OCAP.Api.Hubs;
using OCAP.Api.Services;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Core.Events;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Controllers;

/// <summary>
/// Webhook de Evolution API v2. Cada instancia (una por organización) lo llama con la cabecera
/// <see cref="EvolutionInstanceIdentity.SignatureHeader"/>, que se verifica antes de tocar ningún dato.
/// Siempre responde 200 a eventos válidos, aunque no se procesen: Evolution reintenta todo lo que no sea 2xx.
/// </summary>
[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
public sealed class EvolutionWhatsAppWebhookController : ControllerBase
{
    /// <summary>Evento de SignalR que escucha el panel mientras se vincula WhatsApp.</summary>
    public const string LinkUpdatedEvent = "WhatsAppLinkUpdated";

    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromHours(6);

    private readonly WhatsAppSettings _settings;
    private readonly WhatsAppLinkService _link;
    private readonly ChannelConversationService _conversations;
    private readonly EvolutionApiClient _evolution;
    private readonly IHubContext<EventsHub> _hub;
    private readonly IMemoryCache _processedMessages;
    private readonly ILogger<EvolutionWhatsAppWebhookController> _logger;
    private readonly IEventBus? _eventBus;

    public EvolutionWhatsAppWebhookController(
        IOptions<WhatsAppSettings> settings,
        WhatsAppLinkService link,
        ChannelConversationService conversations,
        EvolutionApiClient evolution,
        IHubContext<EventsHub> hub,
        IMemoryCache processedMessages,
        ILogger<EvolutionWhatsAppWebhookController> logger,
        IEventBus? eventBus = null)
    {
        _settings = settings.Value;
        _link = link;
        _conversations = conversations;
        _evolution = evolution;
        _hub = hub;
        _processedMessages = processedMessages;
        _logger = logger;
        _eventBus = eventBus;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var secret = _settings.EffectiveWebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Falla cerrado: sin API key de Evolution no hay clave para verificar quién llama.
            _logger.LogError("Webhook de WhatsApp rechazado: falta la API key de Evolution (EVOLUTION_API_KEY).");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "WhatsApp no está configurado");
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        string? instance;
        EvolutionWebhookEvent? webhookEvent;
        try
        {
            instance = EvolutionWebhookParser.ReadInstance(body);
            webhookEvent = EvolutionWebhookParser.Parse(body);
        }
        catch (JsonException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Payload de Evolution inválido");
        }

        // Toda llamada debe venir firmada por su instancia, aunque luego el evento no se procese.
        var signature = Request.Headers[EvolutionInstanceIdentity.SignatureHeader].FirstOrDefault();
        if (!EvolutionInstanceIdentity.TryParseTenant(instance, out var tenantId)
            || !EvolutionInstanceIdentity.IsValidSignature(instance!, signature, secret))
        {
            _logger.LogWarning("Webhook de WhatsApp rechazado: instancia desconocida o firma inválida.");
            return Unauthorized();
        }

        if (webhookEvent is null)
        {
            return Ok();
        }

        // Firma verificada: desde aquí todo lo que se lea o escriba queda en la organización de la instancia.
        HttpTenantContext.UseTrustedTenant(HttpContext, tenantId);

        switch (webhookEvent)
        {
            case EvolutionQrUpdatedEvent qr:
                await NotifyPanelAsync(tenantId, new WhatsAppLinkStatus(WhatsAppLinkStates.Pending, qr.Qr.Base64, qr.Qr.PairingCode), cancellationToken);
                break;
            case EvolutionConnectionUpdatedEvent update:
                var status = await _link.ApplyConnectionUpdateAsync(tenantId, update, cancellationToken);
                if (status is not null) await NotifyPanelAsync(tenantId, status, cancellationToken);
                break;
            case EvolutionTextMessageEvent message:
                await ReplyAsync(tenantId, message, cancellationToken);
                break;
        }

        return Ok();
    }

    private async Task ReplyAsync(Guid tenantId, EvolutionTextMessageEvent message, CancellationToken cancellationToken)
    {
        // Evolution reintenta un webhook si tarda o falla: el mismo mensaje no debe responderse dos veces.
        var deduplicationKey = $"whatsapp-message:{message.Instance}:{message.MessageId}";
        if (_processedMessages.TryGetValue(deduplicationKey, out _)) return;
        _processedMessages.Set(deduplicationKey, true, DeduplicationWindow);

        if (await _link.FindConnectionAsync(tenantId, cancellationToken) is null)
        {
            _logger.LogInformation("Mensaje de WhatsApp ignorado: la organización {TenantId} no tiene WhatsApp vinculado.", tenantId);
            return;
        }

        var reply = await _conversations.HandleAsync(
            new InboundChannelMessage(
                tenantId,
                "WhatsApp",
                UserIdFor(tenantId, message.From),
                message.From,
                string.IsNullOrWhiteSpace(message.SenderName) ? $"+{message.From}" : message.SenderName,
                message.Text),
            cancellationToken);

        var sent = false;
        try
        {
            await _evolution.SendTextAsync(message.Instance, message.From, reply.Text, cancellationToken);
            sent = true;
        }
        catch (Exception ex) when (ex is EvolutionApiException or HttpRequestException)
        {
            _logger.LogError(ex, "No se pudo enviar la respuesta de WhatsApp de la organización {TenantId}.", tenantId);
        }

        if (_eventBus is not null)
        {
            await _eventBus.PublishAsync(new MessageSentEvent("WhatsApp", message.From, reply.Text, sent, tenantId), cancellationToken);
        }
    }

    private Task NotifyPanelAsync(Guid tenantId, WhatsAppLinkStatus status, CancellationToken cancellationToken) =>
        _hub.Clients.Group(EventsHub.GetTenantGroupName(tenantId)).SendAsync(LinkUpdatedEvent, status, cancellationToken);

    /// <summary>Contacto estable por organización y número: el mismo número en otra organización es otro contacto.</summary>
    public static Guid UserIdFor(Guid tenantId, string phoneNumber)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"whatsapp:{tenantId:N}:{phoneNumber}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
