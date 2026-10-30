using OCAP.Api.Services;
using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.WhatsApp.Evolution;
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
    /// <summary>App Secret de la app de Meta: firma los webhooks. Se guarda cifrado en la conexión.</summary>
    public string AppSecret { get; set; } = string.Empty;
}

[ApiController]
[Route("api/channels/whatsapp")]
[Authorize(Policy = OcapPermissions.ChannelManage)]
public class WhatsAppChannelController : ControllerBase
{
    private readonly IWhatsAppRuntimeManager _whatsAppRuntime;
    private readonly WhatsAppLinkService _link;
    private readonly EvolutionApiClient _evolution;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WhatsAppChannelController> _logger;

    public WhatsAppChannelController(
        IWhatsAppRuntimeManager whatsAppRuntime,
        WhatsAppLinkService link,
        EvolutionApiClient evolution,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService auditService,
        ILogger<WhatsAppChannelController> logger)
    {
        _whatsAppRuntime = whatsAppRuntime;
        _link = link;
        _evolution = evolution;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Vincula el WhatsApp de la organización con Evolution: devuelve el QR para escanear desde el teléfono
    /// (WhatsApp → Dispositivos vinculados), o el estado vinculado si ya lo estaba. Es idempotente.
    /// </summary>
    [HttpPost("link")]
    [ProducesResponseType(typeof(WhatsAppLinkStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> Link(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) return Unauthorized();

        try
        {
            var status = await _link.LinkAsync(tenantId, cancellationToken);
            await _auditService.LogSecurityEventAsync(
                tenantId,
                _userContext.UserId,
                "WhatsAppLinkRequested",
                "Se solicitó vincular WhatsApp por QR.",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                true,
                cancellationToken);
            return Ok(status);
        }
        catch (WhatsAppNotConfiguredException ex)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "WhatsApp no está configurado", detail: ex.Message);
        }
        catch (Exception ex) when (ex is EvolutionApiException or HttpRequestException)
        {
            _logger.LogError(ex, "Evolution API falló al vincular WhatsApp.");
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "No se pudo contactar el servicio de WhatsApp",
                detail: "Comprueba que el contenedor evolution-api esté en marcha e inténtalo de nuevo.");
        }
    }

    /// <summary>Estado de la vinculación; con <c>qr=true</c> incluye el QR vigente mientras está pendiente.</summary>
    [HttpGet("link")]
    [ProducesResponseType(typeof(WhatsAppLinkStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLink([FromQuery] bool qr, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) return Unauthorized();

        try
        {
            return Ok(await _link.GetStatusAsync(tenantId, qr, cancellationToken));
        }
        catch (Exception ex) when (ex is EvolutionApiException or HttpRequestException)
        {
            _logger.LogError(ex, "Evolution API falló al consultar el estado de WhatsApp.");
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "No se pudo contactar el servicio de WhatsApp");
        }
    }

    /// <summary>Desvincula el número: cierra la sesión en el teléfono y borra la instancia.</summary>
    [HttpDelete("link")]
    public async Task<IActionResult> Unlink(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) return Unauthorized();

        try
        {
            await _link.UnlinkAsync(tenantId, cancellationToken);
        }
        catch (Exception ex) when (ex is EvolutionApiException or HttpRequestException)
        {
            _logger.LogError(ex, "Evolution API falló al desvincular WhatsApp.");
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "No se pudo contactar el servicio de WhatsApp");
        }

        await _auditService.LogSecurityEventAsync(
            tenantId,
            _userContext.UserId,
            "WhatsAppUnlinked",
            "WhatsApp desvinculado.",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            true,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectChannel(
        [FromBody] CreateWhatsAppConnectionRequest request,
        [FromServices] ICredentialVault vault,
        [FromServices] OCAP.Channels.Abstractions.Contracts.IChannelConnectionManager connections,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Tenant no identificado.", Data = null });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumberId) || string.IsNullOrWhiteSpace(request.ApiToken)
            || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.AppSecret))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Nombre, Phone number ID, token de acceso y App Secret son requeridos (modo Cloud). Para vincular por QR usa POST /link.", Data = null });
        }

        try
        {
            var connection = await _whatsAppRuntime.RegisterConnectionAsync(
                tenantId,
                request.DisplayName,
                request.PhoneNumberId,
                request.ApiToken,
                cancellationToken);

            // Secretos del webhook por conexión, sin .env: el App Secret cifrado y un token de verificación generado aquí
            // (se muestra una sola vez; se guarda su hash).
            var appSecretRef = await vault.StoreSecretAsync(tenantId, $"whatsapp-app-secret-{connection.Id:N}", request.AppSecret.Trim(), cancellationToken);
            var verifyToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            await connections.SetMetadataValueAsync(tenantId, connection.Id, WhatsAppCloudSecrets.AppSecretRefKey, appSecretRef, cancellationToken);
            await connections.SetMetadataValueAsync(tenantId, connection.Id, WhatsAppCloudSecrets.VerifyTokenHashKey, WhatsAppCloudSecrets.Hash(verifyToken), cancellationToken);

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
                Message = "Canal de WhatsApp Cloud registrado. Copia el token de verificación en Meta: no se vuelve a mostrar.",
                Data = new { connection.Id, connection.DisplayName, verifyToken, webhookPath = "/api/channels/whatsapp/webhook" }
            });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _logger.LogError(ex, "Error al registrar canal de WhatsApp Cloud.");
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, Data = null });
        }
    }

    /// <summary>Salud del servicio de WhatsApp: con <c>phoneNumberId</c> y <c>apiToken</c> valida WhatsApp Cloud (Meta).</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string? phoneNumberId, [FromQuery] string? apiToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumberId) && !string.IsNullOrWhiteSpace(apiToken))
        {
            var cloud = await _whatsAppRuntime.HealthCheckAsync(phoneNumberId, apiToken, cancellationToken);
            return Ok(new ApiResponse<WhatsAppHealthResultDto> { Success = cloud.IsHealthy, Message = cloud.StatusMessage, Data = cloud });
        }

        return await Health(cancellationToken);
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var configured = _link.IsConfigured;
        var reachable = configured && await _evolution.PingAsync(cancellationToken);
        var message = !configured
            ? "WhatsApp no está configurado en el servidor."
            : reachable ? "Evolution API disponible." : "Evolution API no responde.";
        return Ok(new ApiResponse<object>
        {
            Success = reachable,
            Message = message,
            Data = new { provider = "Evolution", configured, reachable }
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
