using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/webhooks/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppWebhookValidator _validator;
    private readonly WhatsAppMessageReceiver _receiver;

    public WhatsAppWebhookController(
        WhatsAppWebhookValidator validator,
        WhatsAppMessageReceiver receiver)
    {
        _validator = validator;
        _receiver = receiver;
    }

    // Endpoint webhook para recibir notificaciones de eventos desde Evolution API (WhatsApp).
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] WhatsAppWebhookPayload payload,
        [FromHeader(Name = "x-webhook-secret")] string? secretHeader,
        CancellationToken cancellationToken)
    {
        // 1. Validar token o secreto de seguridad opcional en headers.
        if (!_validator.ValidateSecret(secretHeader))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Firma o token de webhook inválido.",
                Data = null
            });
        }

        // 2. Validar estructura del payload recibido.
        if (!_validator.ValidatePayload(payload))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Payload de webhook de WhatsApp inválido o no procesable.",
                Data = null
            });
        }

        // 3. Mapear payload de Evolution API al modelo interno agnóstico de OCAP.
        var incomingMessage = WhatsAppWebhookMapper.ToIncomingMessage(payload);

        // 4. Entregar al receptor del canal WhatsApp.
        var processed = await _receiver.ReceiveMessageAsync(incomingMessage, cancellationToken);

        if (!processed)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Falló el procesamiento interno del mensaje de WhatsApp.",
                Data = null
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Webhook de WhatsApp procesado exitosamente.",
            Data = null
        });
    }
}
