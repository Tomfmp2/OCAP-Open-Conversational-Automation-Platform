using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public class ValidateWhatsAppTokenRequest
{
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}

public class CreateWhatsAppConnectionRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}

[ApiController]
[Route("api/channels/whatsapp")]
public class WhatsAppChannelController : ControllerBase
{
    private readonly IWhatsAppRuntimeManager _whatsAppRuntime;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WhatsAppChannelController> _logger;

    public WhatsAppChannelController(
        IWhatsAppRuntimeManager whatsAppRuntime,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService auditService,
        ILogger<WhatsAppChannelController> logger)
    {
        _whatsAppRuntime = whatsAppRuntime;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectChannel([FromBody] CreateWhatsAppConnectionRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumberId) || string.IsNullOrWhiteSpace(request.ApiToken) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "DisplayName, PhoneNumberId y ApiToken son requeridos.", Data = null });
        }

        try
        {
            var connection = await _whatsAppRuntime.RegisterConnectionAsync(
                tenantId,
                request.DisplayName,
                request.PhoneNumberId,
                request.ApiToken,
                cancellationToken);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogSecurityEventAsync(
                tenantId,
                userId,
                "WhatsAppChannelCreated",
                $"Canal de WhatsApp {request.DisplayName} creado para PhoneNumberId {request.PhoneNumberId}.",
                ipAddress,
                true,
                cancellationToken);

            return Created($"/api/channels/connections/{connection.Id}", new ApiResponse<object>
            {
                Success = true,
                Message = "Canal de WhatsApp registrado exitosamente.",
                Data = connection
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar canal de WhatsApp.");
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, Data = null });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string phoneNumberId, [FromQuery] string apiToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(apiToken))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "PhoneNumberId y ApiToken son requeridos.", Data = null });
        }

        var result = await _whatsAppRuntime.HealthCheckAsync(phoneNumberId, apiToken, cancellationToken);
        return Ok(new ApiResponse<WhatsAppHealthResultDto>
        {
            Success = result.IsHealthy,
            Message = result.StatusMessage,
            Data = result
        });
    }

    [HttpPost("disconnect/{id:guid}")]
    public async Task<IActionResult> DisconnectChannel(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        var success = await _whatsAppRuntime.DeleteConnectionAsync(tenantId, id, cancellationToken);
        if (!success)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Conexión de WhatsApp no encontrada.", Data = null });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogSecurityEventAsync(
            tenantId,
            userId,
            "WhatsAppChannelDeleted",
            $"Canal de WhatsApp con ID {id} eliminado.",
            ipAddress,
            true,
            cancellationToken);

        return Ok(new ApiResponse<object> { Success = true, Message = "Canal de WhatsApp eliminado exitosamente.", Data = null });
    }
}
