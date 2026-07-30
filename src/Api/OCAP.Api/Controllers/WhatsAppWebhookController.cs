using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/channels/whatsapp/webhook")]
[AllowAnonymous]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppWebhookValidator _validator;
    private readonly WhatsAppMessageReceiver _receiver;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        WhatsAppWebhookValidator validator,
        WhatsAppMessageReceiver receiver,
        ILogger<WhatsAppWebhookController> logger)
    {
        _validator = validator;
        _receiver = receiver;
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
        
        if (incomingMessage != null)
        {
            // 4. Entregar al receptor del canal WhatsApp.
            var processed = await _receiver.ReceiveMessageAsync(incomingMessage, cancellationToken);
            if (!processed)
            {
                _logger.LogWarning("Falló el procesamiento interno del mensaje de WhatsApp.");
            }
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Webhook de WhatsApp procesado exitosamente.",
            Data = null
        });
    }
}
