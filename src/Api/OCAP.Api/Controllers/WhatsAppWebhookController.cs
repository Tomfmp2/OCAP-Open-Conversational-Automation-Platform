using OCAP.Api.Services;
using OCAP.Core.Events.Distributed;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Channels.WhatsApp.Webhooks;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/channels/whatsapp/webhook")]
[AllowAnonymous]
[DisableRateLimiting]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppWebhookValidator _validator;
    private readonly IChannelConnectionManager _connections;
    private readonly IConversationResponder _responder;
    private readonly WhatsAppApiClient _cloudClient;
    private readonly IInboxStore _inbox;
    private readonly OCAP.Security.Abstractions.ICredentialVault _vault;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        WhatsAppWebhookValidator validator,
        IChannelConnectionManager connections,
        IConversationResponder responder,
        WhatsAppApiClient cloudClient,
        IInboxStore inbox,
        OCAP.Security.Abstractions.ICredentialVault vault,
        ILogger<WhatsAppWebhookController> logger)
    {
        _validator = validator;
        _connections = connections;
        _responder = responder;
        _cloudClient = cloudClient;
        _inbox = inbox;
        _vault = vault;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        CancellationToken cancellationToken)
    {
        if (mode != "subscribe" || string.IsNullOrWhiteSpace(verifyToken)) return Forbid();

        // El token del servidor (.env) o el que generó el panel al conectar un número Cloud.
        var valid = _validator.ValidateVerifyToken(mode, verifyToken)
            || (await CloudConnectionsAsync(cancellationToken))
                .Any(c => WhatsAppCloudSecrets.Matches(verifyToken, c.ConfigurationMetadata.GetValueOrDefault(WhatsAppCloudSecrets.VerifyTokenHashKey)));
        return valid ? Ok(challenge) : Forbid();
    }

    private async Task<IReadOnlyList<OCAP.Security.Domain.Entities.ChannelConnection>> CloudConnectionsAsync(CancellationToken cancellationToken) =>
        (await _connections.GetAllConnectionsByProviderAsync("WhatsApp", cancellationToken))
            .Where(c => c.ConfigurationMetadata.GetValueOrDefault("ConnectionMode") == "cloud")
            .ToList();

    /// <summary>
    /// Firma de Meta con el App Secret del número que recibe el mensaje (guardado cifrado al conectarlo) o con el del
    /// servidor. Se lee el phone_number_id del cuerpo antes de verificar, pero nada se procesa hasta que la firma vale.
    /// </summary>
    private async Task<bool> SignatureIsValidAsync(string rawBody, string? signatureHeader, WhatsAppCloudWebhookPayload? payload, CancellationToken cancellationToken)
    {
        if (_validator.ValidateSignature(rawBody, signatureHeader)) return true;

        var phoneNumberId = payload?.Entry.SelectMany(e => e.Changes).Select(c => c.Value.Metadata?.PhoneNumberId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        if (phoneNumberId is null) return false;
        var connection = (await CloudConnectionsAsync(cancellationToken))
            .FirstOrDefault(c => c.ConfigurationMetadata.GetValueOrDefault("PhoneNumberId") == phoneNumberId);
        if (connection?.ConfigurationMetadata.GetValueOrDefault(WhatsAppCloudSecrets.AppSecretRefKey) is not { } reference) return false;

        var secret = await _vault.RetrieveSecretAsync(connection.TenantId, reference, cancellationToken);
        return secret is not null && WhatsAppWebhookValidator.SignatureMatches(rawBody, signatureHeader, secret);
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        WhatsAppCloudWebhookPayload? payload = null;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<WhatsAppCloudWebhookPayload>(
                rawBody, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Error deserializando payload de WhatsApp.");
        }

        // 1. Firma de Meta antes de usar nada del cuerpo.
        if (!await SignatureIsValidAsync(rawBody, signatureHeader, payload, cancellationToken))
        {
            _logger.LogWarning("Firma del webhook de WhatsApp Cloud no válida o ausente.");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Firma o token de webhook inválido.",
                Data = null
            });
        }

        if (payload == null)
        {
            return BadRequest();
        }

        // 2. Validar estructura del payload recibido.
        if (!_validator.ValidatePayload(payload))
        {
            return Ok(); // WhatsApp requiere 200 OK incluso si no se procesa
        }

        // 3. Mapear payload de WhatsApp Cloud API al modelo interno agnóstico de OCAP.
        var incomingMessage = WhatsAppWebhookMapper.ToIncomingMessage(payload);
        
        if (incomingMessage != null && !string.IsNullOrWhiteSpace(incomingMessage.Message))
        {
            await ReplyAsync(incomingMessage, cancellationToken);
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Webhook de WhatsApp procesado exitosamente.",
            Data = null
        });
    }

    /// <summary>
    /// La firma de Meta ya se verificó; la organización sale de la conexión Cloud con ese PhoneNumberId (único por
    /// registro). Meta reintenta los webhooks: cada mensaje se responde una sola vez.
    /// </summary>
    private async Task ReplyAsync(OCAP.Channels.Abstractions.Models.IncomingChannelMessage message, CancellationToken cancellationToken)
    {
        var phoneNumberId = message.Metadata.GetValueOrDefault("PhoneNumberId");
        var connection = (await _connections.GetEnabledConnectionsByProviderAsync("WhatsApp", cancellationToken))
            .Where(c => c.ConfigurationMetadata.GetValueOrDefault("ConnectionMode") == "cloud"
                        && c.ConfigurationMetadata.GetValueOrDefault("PhoneNumberId") == phoneNumberId)
            .ToList();
        if (string.IsNullOrWhiteSpace(phoneNumberId) || connection.Count != 1)
        {
            _logger.LogInformation("Mensaje de WhatsApp Cloud ignorado: el número {PhoneNumberId} no está conectado.", phoneNumberId);
            return;
        }

        var key = $"whatsapp-cloud-message:{message.Metadata.GetValueOrDefault("MessageId")}";
        if (!await _inbox.TryClaimAsync(key, ChannelInbox.ConsumerGroup, cancellationToken)) return;

        var tenantId = connection[0].TenantId;
        HttpTenantContext.UseTrustedTenant(HttpContext, tenantId);
        var token = await _connections.GetCredentialsAsync(connection[0], cancellationToken);
        var reply = await _responder.RespondAsync(
            tenantId, connection[0].Id, message, message.Metadata.GetValueOrDefault("SenderName") ?? string.Empty, cancellationToken);
        if (reply is null) return; // una persona del equipo atiende la conversación

        var sent = await _cloudClient.SendMessageAsync(
            phoneNumberId,
            new WhatsAppCloudSendMessageRequest { To = message.ExternalUserId, Text = new WhatsAppCloudText { Body = reply } },
            token,
            cancellationToken);
        if (!sent)
        {
            _logger.LogWarning("No se pudo enviar la respuesta de WhatsApp Cloud de la organización {TenantId}.", tenantId);
        }
    }
}
