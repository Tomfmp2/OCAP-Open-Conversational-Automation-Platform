using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Channels.Telegram.Services;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/webhooks/telegram")]
[AllowAnonymous]
public class TelegramWebhookController : ControllerBase
{
    private readonly TelegramWebhookValidator _validator;
    private readonly TelegramMessageReceiver _receiver;

    public TelegramWebhookController(
        TelegramWebhookValidator validator,
        TelegramMessageReceiver receiver)
    {
        _validator = validator;
        _receiver = receiver;
    }

    // Endpoint webhook HTTP POST para la recepción de eventos provenientes de Telegram Bot API.
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] TelegramUpdate update,
        [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string? secretHeader,
        CancellationToken cancellationToken)
    {
        // 1. Validar secreto de seguridad en encabezado HTTP.
        if (!_validator.ValidateSecret(secretHeader))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Firma o token de secreto de webhook de Telegram inválido.",
                Data = null
            });
        }

        // 2. Validar integridad de la actualización (Update) de Telegram.
        if (!_validator.ValidatePayload(update))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Payload de actualización de Telegram inválido o sin contenido de texto.",
                Data = null
            });
        }

        // 3. Mapear DTO nativo de Telegram al modelo agnóstico inmutable de OCAP.
        var incomingMessage = TelegramWebhookMapper.ToIncomingMessage(update);

        // 4. Entregar al receptor del canal Telegram para su procesamiento en el pipeline conversacional.
        var processed = await _receiver.ReceiveMessageAsync(incomingMessage, cancellationToken);

        if (!processed)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Falló el procesamiento interno del mensaje de Telegram.",
                Data = null
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Webhook de Telegram procesado exitosamente.",
            Data = null
        });
    }
}
