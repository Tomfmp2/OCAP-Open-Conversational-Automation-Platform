using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppWebhookValidator _validator;
    private readonly IChannelConnectionManager _connections;
    private readonly IConversationResponder _responder;
    private readonly WhatsAppApiClient _cloudClient;
    private readonly IMemoryCache _processedMessages;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        WhatsAppWebhookValidator validator,
        IChannelConnectionManager connections,
        IConversationResponder responder,
        WhatsAppApiClient cloudClient,
        IMemoryCache processedMessages,
        ILogger<WhatsAppWebhookController> logger)
    {
        _validator = validator;
        _connections = connections;
        _responder = responder;
        _cloudClient = cloudClient;
        _processedMessages = processedMessages;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (!string.IsNullOrWhiteSpace(mode) && !string.IsNullOrWhiteSpace(verifyToken))
        {
            if (_validator.ValidateVerifyToken(mode, verifyToken))
            {
                return Ok(challenge);
            }
        }

        return Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        // 1. Validar token o secreto de seguridad en headers.
        if (!_validator.ValidateSignature(rawBody, signatureHeader))
        {
            _logger.LogWarning("Token secreto de Webhook de WhatsApp no válido o ausente.");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Firma o token de webhook inválido.",
                Data = null
            });
        }

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
        if (_processedMessages.TryGetValue(key, out _)) return;
        _processedMessages.Set(key, true, TimeSpan.FromHours(6));

        var tenantId = connection[0].TenantId;
        HttpTenantContext.UseTrustedTenant(HttpContext, tenantId);
        var token = await _connections.GetCredentialsAsync(connection[0], cancellationToken);
        var reply = await _responder.RespondAsync(
            tenantId, message, message.Metadata.GetValueOrDefault("SenderName") ?? string.Empty, cancellationToken);

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
