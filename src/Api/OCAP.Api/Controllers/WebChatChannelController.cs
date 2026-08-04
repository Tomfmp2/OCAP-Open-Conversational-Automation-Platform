using Microsoft.AspNetCore.Mvc;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Application.Services;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WebChat.Services;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public class CreateWebChatConnectionRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? WidgetTitle { get; set; }
}

public class WebChatSendMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

[ApiController]
[Route("api/channels/webchat")]
public class WebChatChannelController : ControllerBase
{
    private readonly IWebChatRuntimeManager _runtime;
    private readonly WebChatMessageReceiver _receiver;
    private readonly WebChatMessageSender _sender;
    private readonly IEnterpriseAssistantAgent _assistantAgent;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WebChatChannelController> _logger;

    public WebChatChannelController(
        IWebChatRuntimeManager runtime,
        WebChatMessageReceiver receiver,
        WebChatMessageSender sender,
        IEnterpriseAssistantAgent assistantAgent,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService auditService,
        ILogger<WebChatChannelController> logger)
    {
        _runtime = runtime;
        _receiver = receiver;
        _sender = sender;
        _assistantAgent = assistantAgent;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] CreateWebChatConnectionRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "DisplayName es requerido.", Data = null });
        }

        try
        {
            var connection = await _runtime.RegisterConnectionAsync(
                tenantId,
                request.DisplayName.Trim(),
                request.WidgetTitle,
                cancellationToken);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogSecurityEventAsync(
                tenantId,
                _userContext.UserId,
                "WebChatChannelCreated",
                $"Canal WebChat {request.DisplayName} creado.",
                ipAddress,
                true,
                cancellationToken);

            return Created($"/api/channels/connections/{connection.Id}", new ApiResponse<object>
            {
                Success = true,
                Message = "Canal WebChat registrado.",
                Data = connection
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar WebChat.");
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, Data = null });
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        var health = await _runtime.GetHealthAsync(tenantId, cancellationToken);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Estado WebChat.",
            Data = health
        });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] WebChatSendMessageRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "SessionId y Message son requeridos.",
                Data = null
            });
        }

        var sessionId = request.SessionId.Trim();
        var incoming = new IncomingChannelMessage
        {
            ExternalUserId = sessionId,
            Message = request.Message.Trim(),
            ChannelName = "WebChat",
            Metadata =
            {
                ["TenantId"] = tenantId.ToString(),
                ["DisplayName"] = string.IsNullOrWhiteSpace(request.DisplayName) ? $"WebChat {sessionId}" : request.DisplayName.Trim()
            }
        };

        var processed = await _receiver.ReceiveMessageAsync(incoming, cancellationToken);
        if (!processed)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "No se pudo registrar el mensaje entrante.",
                Data = null
            });
        }

        var userId = WebChatMessageReceiver.ResolveUserId(sessionId);
        string replyText;
        string providerUsed = "fallback";

        try
        {
            var context = new AgentContext(
                _assistantAgent.GlobalAgentId,
                tenantId,
                userId,
                request.Message.Trim());

            var result = await _assistantAgent.ProcessRequestAsync(context, cancellationToken);
            replyText = string.IsNullOrWhiteSpace(result.OutputMessage)
                ? "He recibido tu mensaje."
                : result.OutputMessage;
            providerUsed = result.ProviderUsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Enterprise Assistant no disponible; respuesta de respaldo WebChat.");
            replyText = $"Recibí tu mensaje: «{request.Message.Trim()}». El asistente IA no está disponible ahora.";
        }

        await _sender.SendMessageAsync(new OutgoingChannelMessage
        {
            DestinationUserId = sessionId,
            Message = replyText,
            ChannelName = "WebChat",
            Metadata = { ["TenantId"] = tenantId.ToString() }
        }, cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Mensaje procesado.",
            Data = new
            {
                SessionId = sessionId,
                Reply = replyText,
                ProviderUsed = providerUsed
            }
        });
    }
}
