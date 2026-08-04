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

public class CreateWhatsAppQrRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? InstanceName { get; set; }
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

    /// <summary>Conexión fácil vía Evolution API: crea instancia y devuelve QR para escanear.</summary>
    [HttpPost("connect-qr")]
    public async Task<IActionResult> ConnectWithQr([FromBody] CreateWhatsAppQrRequest request, CancellationToken cancellationToken)
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
            var result = await _whatsAppRuntime.ConnectWithQrAsync(
                tenantId,
                request.DisplayName.Trim(),
                request.InstanceName,
                cancellationToken);

            await _auditService.LogSecurityEventAsync(
                tenantId,
                _userContext.UserId,
                "WhatsAppEvolutionQrCreated",
                $"WhatsApp Evolution instance {result.InstanceName} creada.",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                true,
                cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Escanea el QR con WhatsApp (Dispositivos vinculados).",
                Data = new
                {
                    connectionId = result.Connection.Id,
                    instanceName = result.InstanceName,
                    qrBase64 = result.Qr.Base64,
                    qrCode = result.Qr.Code,
                    pairingCode = result.Qr.PairingCode,
                    status = result.Qr.Status
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connect-qr WhatsApp Evolution.");
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, Data = null });
        }
    }

    [HttpGet("qr/{instanceName}")]
    public async Task<IActionResult> RefreshQr(string instanceName, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        var qr = await _whatsAppRuntime.RefreshQrAsync(instanceName, cancellationToken);
        return Ok(new ApiResponse<object>
        {
            Success = !string.IsNullOrWhiteSpace(qr.Base64),
            Message = "QR Evolution",
            Data = new { instanceName = qr.InstanceName, qrBase64 = qr.Base64, qrCode = qr.Code, pairingCode = qr.PairingCode, status = qr.Status }
        });
    }

    [HttpGet("evolution/state/{instanceName}")]
    public async Task<IActionResult> EvolutionState(string instanceName, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        var state = await _whatsAppRuntime.GetEvolutionStateAsync(instanceName, cancellationToken);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = state.State,
            Data = new { instanceName = state.InstanceName, state = state.State, isOpen = state.IsOpen }
        });
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
            return BadRequest(new ApiResponse<object> { Success = false, Message = "DisplayName, PhoneNumberId y ApiToken son requeridos (modo Cloud). Para QR usa POST /connect-qr.", Data = null });
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
                $"Canal de WhatsApp Cloud {request.DisplayName} creado.",
                ipAddress,
                true,
                cancellationToken);

            return Created($"/api/channels/connections/{connection.Id}", new ApiResponse<object>
            {
                Success = true,
                Message = "Canal de WhatsApp Cloud registrado.",
                Data = connection
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar canal de WhatsApp Cloud.");
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, Data = null });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string? phoneNumberId, [FromQuery] string? apiToken, CancellationToken cancellationToken)
    {
        WhatsAppHealthResultDto result;
        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(apiToken))
        {
            result = await _whatsAppRuntime.HealthCheckEvolutionAsync(cancellationToken);
        }
        else
        {
            result = await _whatsAppRuntime.HealthCheckAsync(phoneNumberId, apiToken, cancellationToken);
        }

        return Ok(new ApiResponse<WhatsAppHealthResultDto>
        {
            Success = result.IsHealthy,
            Message = result.StatusMessage,
            Data = result
        });
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var result = await _whatsAppRuntime.HealthCheckEvolutionAsync(cancellationToken);
        return Ok(new ApiResponse<object>
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

        return Ok(new ApiResponse<object> { Success = true, Message = "Canal de WhatsApp eliminado.", Data = null });
    }
}
