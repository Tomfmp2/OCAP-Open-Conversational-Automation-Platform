using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Channels.Telegram.Services;
using OCAP.Channels.Telegram.Webhooks;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public class ValidateTelegramTokenRequest
{
    public string BotToken { get; set; } = string.Empty;
}

public class CreateTelegramBotRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string BotToken { get; set; } = string.Empty;
    public string ConnectionMode { get; set; } = "webhook"; // webhook | polling
    public string? WebhookBaseUrl { get; set; }
}

[ApiController]
[Route("api/channels/telegram")]
public class TelegramChannelController : ControllerBase
{
    private readonly ITelegramBotRuntimeManager _telegramRuntime;
    private readonly TelegramWebhookValidator _webhookValidator;
    private readonly TelegramMessageReceiver _messageReceiver;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<TelegramChannelController> _logger;

    public TelegramChannelController(
        ITelegramBotRuntimeManager telegramRuntime,
        TelegramWebhookValidator webhookValidator,
        TelegramMessageReceiver messageReceiver,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService auditService,
        ILogger<TelegramChannelController> logger)
    {
        _telegramRuntime = telegramRuntime;
        _webhookValidator = webhookValidator;
        _messageReceiver = messageReceiver;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
    }

    // POST /api/channels/telegram/validate-token - Valida la autenticidad de un Bot Token con Telegram getMe.
    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTelegramTokenRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.BotToken))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "El BotToken es obligatorio.", Data = null });
        }

        var botInfo = await _telegramRuntime.ValidateTokenAsync(request.BotToken, cancellationToken);
        if (botInfo == null)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Bot Token inválido o rechazada la comunicación con Telegram API.", Data = null });
        }

        return Ok(new ApiResponse<TelegramBotInfoDto>
        {
            Success = true,
            Message = "Token de Bot de Telegram validado exitosamente.",
            Data = botInfo
        });
    }

    // POST /api/channels/telegram/bots - Registra un nuevo bot de Telegram para el tenant activo.
    [HttpPost("bots")]
    public async Task<IActionResult> CreateBot([FromBody] CreateTelegramBotRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.BotToken) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "DisplayName y BotToken son requeridos.", Data = null });
        }

        try
        {
            var baseUrl = request.WebhookBaseUrl ?? $"{Request.Scheme}://{Request.Host}";
            var connection = await _telegramRuntime.RegisterBotAsync(
                tenantId,
                request.DisplayName,
                request.BotToken,
                request.ConnectionMode,
                baseUrl,
                cancellationToken);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogSecurityEventAsync(
                tenantId,
                userId,
                "Telegram_Bot_Registered",
                $"Bot de Telegram {request.DisplayName} registrado con modo {request.ConnectionMode}.",
                ipAddress,
                true,
                cancellationToken);

            return Created($"/api/channels/connections/{connection.Id}", new ApiResponse<object>
            {
                Success = true,
                Message = "Bot de Telegram registrado exitosamente.",
                Data = connection
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar bot de Telegram.");
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, Data = null });
        }
    }

    // POST /api/channels/telegram/webhook - Endpoint de Webhook entrante invocado por Telegram servers.
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        var secretHeader = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();

        if (!_webhookValidator.ValidateSecret(secretHeader))
        {
            _logger.LogWarning("Token secreto de Webhook de Telegram no válido o ausente.");
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Secret token inválido.", Data = null });
        }

        if (update == null)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Update payload nulo.", Data = null });
        }

        var incomingMessage = TelegramWebhookMapper.ToIncomingMessage(update);
        if (incomingMessage != null)
        {
            await _messageReceiver.ReceiveMessageAsync(incomingMessage, cancellationToken);
        }

        return Ok(new ApiResponse<object> { Success = true, Message = "Update recibido procesado correctamente.", Data = null });
    }

    // GET /api/channels/telegram/bots/{id}/health - Health check y latencia de conexión del bot.
    [HttpGet("bots/{id:guid}/health")]
    public async Task<IActionResult> GetBotHealth(Guid id, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await _telegramRuntime.HealthCheckAsync(token, cancellationToken);
        return Ok(new ApiResponse<TelegramHealthResultDto>
        {
            Success = result.IsHealthy,
            Message = result.StatusMessage,
            Data = result
        });
    }

    // POST /api/channels/telegram/bots/{id}/reconnect - Re-conecta/re-inicializa la conexión del bot.
    [HttpPost("bots/{id:guid}/reconnect")]
    public async Task<IActionResult> ReconnectBot(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        var success = await _telegramRuntime.ReconnectAsync(tenantId, id, cancellationToken);
        if (!success)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Conexión de bot no encontrada.", Data = null });
        }

        return Ok(new ApiResponse<object> { Success = true, Message = "Reconexión de bot de Telegram completada exitosamente.", Data = null });
    }

    // DELETE /api/channels/telegram/bots/{id} - Elimina el bot de Telegram y desregistra el webhook.
    [HttpDelete("bots/{id:guid}")]
    public async Task<IActionResult> DeleteBot(Guid id, [FromQuery] string? token, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        var success = await _telegramRuntime.DeleteBotAsync(tenantId, id, token ?? string.Empty, cancellationToken);
        if (!success)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Conexión de bot no encontrada.", Data = null });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogSecurityEventAsync(
            tenantId,
            userId,
            "Telegram_Bot_Deleted",
            $"Bot de Telegram {id} eliminado.",
            ipAddress,
            true,
            cancellationToken);

        return Ok(new ApiResponse<object> { Success = true, Message = "Bot de Telegram eliminado exitosamente.", Data = null });
    }
}
