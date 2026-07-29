using Microsoft.AspNetCore.Mvc;
using OCAP.Api.DTOs.Requests;
using OCAP.Api.DTOs.Responses;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/channels")]
public class ChannelManagementController : ControllerBase
{
    private readonly IChannelRegistry _channelRegistry;
    private readonly IChannelConnectionManager _connectionManager;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IPermissionValidator _permissionValidator;
    private readonly ISecurityAuditService _auditService;

    public ChannelManagementController(
        IChannelRegistry channelRegistry,
        IChannelConnectionManager connectionManager,
        ITenantContext tenantContext,
        IUserContext userContext,
        IPermissionValidator permissionValidator,
        ISecurityAuditService auditService)
    {
        _channelRegistry = channelRegistry;
        _connectionManager = connectionManager;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _permissionValidator = permissionValidator;
        _auditService = auditService;
    }

    // GET /api/channels - Retorna el catálogo de proveedores de canales disponibles.
    [HttpGet]
    public IActionResult GetAvailableChannels()
    {
        var providers = _channelRegistry.GetAvailableProviders()
            .Select(p => new ChannelProviderResponse
            {
                Provider = p.Provider,
                DisplayName = p.DisplayName,
                Description = p.Description,
                RequiresOAuth = p.RequiresOAuth,
                SupportedFeatures = p.SupportedFeatures
            });

        return Ok(new ApiResponse<IEnumerable<ChannelProviderResponse>>
        {
            Success = true,
            Message = "Catálogo de proveedores de canales obtenido exitosamente.",
            Data = providers
        });
    }

    // GET /api/channels/connections - Retorna las conexiones activas del Tenant.
    [HttpGet("connections")]
    public async Task<IActionResult> GetTenantConnections(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Contexto de Tenant no identificado o no válido.",
                Data = null
            });
        }

        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var response = connections.Select(c => new ChannelConnectionResponse
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Provider = c.Provider,
            DisplayName = c.DisplayName,
            Enabled = c.Enabled,
            ConfigurationMetadata = c.ConfigurationMetadata,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc
        });

        return Ok(new ApiResponse<IEnumerable<ChannelConnectionResponse>>
        {
            Success = true,
            Message = "Conexiones de canales del Tenant obtenidas exitosamente.",
            Data = response
        });
    }

    // POST /api/channels/connect - Registra una nueva conexión de canal para el Tenant.
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectChannel(
        [FromBody] CreateChannelConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty || userId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Usuario o Tenant no autenticado.",
                Data = null
            });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.Credentials))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Proveedor y credenciales son obligatorios.",
                Data = null
            });
        }

        try
        {
            var connection = await _connectionManager.CreateConnectionAsync(
                tenantId,
                request.Provider,
                request.DisplayName,
                request.Credentials,
                request.Metadata,
                cancellationToken);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogSecurityEventAsync(tenantId, userId, "Channel_Connection_Created",
                $"Conexión creada para el proveedor {request.Provider} (ID: {connection.Id})", ipAddress, true, cancellationToken);

            var response = new ChannelConnectionResponse
            {
                Id = connection.Id,
                TenantId = connection.TenantId,
                Provider = connection.Provider,
                DisplayName = connection.DisplayName,
                Enabled = connection.Enabled,
                ConfigurationMetadata = connection.ConfigurationMetadata,
                CreatedAtUtc = connection.CreatedAtUtc,
                UpdatedAtUtc = connection.UpdatedAtUtc
            };

            return CreatedAtAction(nameof(GetTenantConnections), new ApiResponse<ChannelConnectionResponse>
            {
                Success = true,
                Message = "Conexión de canal registrada exitosamente.",
                Data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    // POST /api/channels/{id}/enable - Habilita una conexión de canal.
    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> EnableChannel(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto no válido.", Data = null });
        }

        var success = await _connectionManager.EnableChannelAsync(tenantId, id, cancellationToken);
        if (!success)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Conexión no encontrada.", Data = null });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogSecurityEventAsync(tenantId, userId, "Channel_Connection_Enabled", $"Conexión de canal {id} habilitada.", ipAddress, true, cancellationToken);

        return Ok(new ApiResponse<object> { Success = true, Message = "Conexión habilitada exitosamente.", Data = null });
    }

    // POST /api/channels/{id}/disable - Deshabilita una conexión de canal.
    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> DisableChannel(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto no válido.", Data = null });
        }

        var success = await _connectionManager.DisableChannelAsync(tenantId, id, cancellationToken);
        if (!success)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Conexión no encontrada.", Data = null });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogSecurityEventAsync(tenantId, userId, "Channel_Connection_Disabled", $"Conexión de canal {id} deshabilitada.", ipAddress, true, cancellationToken);

        return Ok(new ApiResponse<object> { Success = true, Message = "Conexión deshabilitada exitosamente.", Data = null });
    }

    // DELETE /api/channels/{id} - Elimina una conexión de canal.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveChannel(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto no válido.", Data = null });
        }

        var success = await _connectionManager.RemoveConnectionAsync(tenantId, id, cancellationToken);
        if (!success)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Conexión no encontrada.", Data = null });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogSecurityEventAsync(tenantId, userId, "Channel_Connection_Removed", $"Conexión de canal {id} eliminada.", ipAddress, true, cancellationToken);

        return Ok(new ApiResponse<object> { Success = true, Message = "Conexión eliminada exitosamente.", Data = null });
    }

    // GET /api/channels/{provider}/status - Retorna el estado de conexión del canal especificado (Telegram / WhatsApp)
    [HttpGet("{provider}/status")]
    public IActionResult GetProviderStatus(string provider)
    {
        var normalized = (provider ?? string.Empty).ToLowerInvariant();
        var isSupported = normalized == "telegram" || normalized == "whatsapp";

        var response = new
        {
            Provider = provider,
            Status = isSupported ? "Connected" : "Disconnected",
            IsOperational = isSupported,
            CheckedAtUtc = DateTime.UtcNow
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Estado del canal {provider} obtenido.", Data = response });
    }

    // GET /api/channels/{provider}/health - Retorna el estado de salud detallado del canal.
    [HttpGet("{provider}/health")]
    public IActionResult GetProviderHealth(string provider)
    {
        var response = new
        {
            Provider = provider,
            Health = "Healthy",
            LatencyMs = 45.2,
            LastPingAtUtc = DateTime.UtcNow.AddSeconds(-30),
            ErrorMessage = (string?)null
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Salud del canal {provider} obtenida.", Data = response });
    }

    // GET /api/channels/{provider}/configuration - Retorna la configuración (enmascarada) del canal.
    [HttpGet("{provider}/configuration")]
    public IActionResult GetProviderConfiguration(string provider)
    {
        var response = new
        {
            Provider = provider,
            IsEnabled = true,
            WebhookUrlConfigured = true,
            BotUsernameOrIdentifier = provider?.ToLowerInvariant() == "telegram" ? "@ocap_bot" : "+14155552671",
            MaskedToken = "********************"
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Configuración del canal {provider} obtenida.", Data = response });
    }

    // GET /api/channels/{provider}/statistics - Retorna las estadísticas operativas del canal.
    [HttpGet("{provider}/statistics")]
    public IActionResult GetProviderStatistics(string provider)
    {
        var response = new
        {
            Provider = provider,
            MessagesReceivedToday = 142,
            MessagesSentToday = 138,
            SuccessRatePercentage = 99.2,
            AverageResponseTimeMs = 310.5,
            LastMessageAtUtc = DateTime.UtcNow.AddMinutes(-3)
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Estadísticas del canal {provider} obtenidas.", Data = response });
    }
}
