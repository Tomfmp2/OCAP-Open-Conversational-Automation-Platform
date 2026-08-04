using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Api.Controllers;

/// <summary>
/// Webhook para Evolution API (QR / Baileys). URL típica:
/// http://host:5229/api/webhooks/whatsapp
/// </summary>
[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
public class EvolutionWhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppWebhookValidator _validator;
    private readonly WhatsAppMessageReceiver _receiver;
    private readonly ILogger<EvolutionWhatsAppWebhookController> _logger;

    public EvolutionWhatsAppWebhookController(
        WhatsAppWebhookValidator validator,
        WhatsAppMessageReceiver receiver,
        ILogger<EvolutionWhatsAppWebhookController> logger)
    {
        _validator = validator;
        _receiver = receiver;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var secret = Request.Headers["x-webhook-secret"].FirstOrDefault()
                     ?? Request.Headers["X-Webhook-Secret"].FirstOrDefault();
        var apikey = Request.Headers["apikey"].FirstOrDefault();

        if (!_validator.ValidateEvolutionSecret(secret, apikey))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Secreto de webhook Evolution inválido.",
                Data = null
            });
        }

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        WhatsAppWebhookPayload? payload = null;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<WhatsAppWebhookPayload>(
                rawBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payload Evolution no parseable.");
            return Ok(); // Evolution reintenta si no es 200
        }

        if (!_validator.ValidateEvolutionPayload(payload))
        {
            return Ok();
        }

        var incoming = WhatsAppWebhookMapper.ToIncomingMessage(payload!);
        if (incoming != null)
        {
            await _receiver.ReceiveMessageAsync(incoming, cancellationToken);
        }

        return Ok(new ApiResponse<object> { Success = true, Message = "OK", Data = null });
    }
}
