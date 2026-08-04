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
                IsImplemented = p.IsImplemented,
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

    // GET /api/channels/{provider}/status - Estado real seg?n conexiones del tenant
    [HttpGet("{provider}/status")]
    public async Task<IActionResult> GetProviderStatus(string provider, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto de Tenant no v?lido.", Data = null });
        }

        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var match = connections.FirstOrDefault(c =>
            string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));

        var connected = match is { Enabled: true };
        var response = new
        {
            Provider = provider,
            Status = connected ? "Connected" : (match is null ? "NotConfigured" : "Disabled"),
            IsOperational = connected,
            ConnectionId = match?.Id,
            CheckedAtUtc = DateTime.UtcNow
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Estado del canal {provider} obtenido.", Data = response });
    }

    // GET /api/channels/{provider}/health
    [HttpGet("{provider}/health")]
    public async Task<IActionResult> GetProviderHealth(string provider, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto de Tenant no v?lido.", Data = null });
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        sw.Stop();
        var match = connections.FirstOrDefault(c =>
            string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase) && c.Enabled);

        var response = new
        {
            Provider = provider,
            Health = match is null ? "NotConfigured" : "Healthy",
            LatencyMs = sw.Elapsed.TotalMilliseconds,
            LastPingAtUtc = DateTime.UtcNow,
            ErrorMessage = match is null ? "No hay conexi?n habilitada para este proveedor." : (string?)null
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Salud del canal {provider} obtenida.", Data = response });
    }

    // GET /api/channels/{provider}/configuration
    [HttpGet("{provider}/configuration")]
    public async Task<IActionResult> GetProviderConfiguration(string provider, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto de Tenant no v?lido.", Data = null });
        }

        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var match = connections.FirstOrDefault(c =>
            string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));

        var response = new
        {
            Provider = provider,
            IsEnabled = match?.Enabled ?? false,
            WebhookUrlConfigured = match?.ConfigurationMetadata?.ContainsKey("WebhookUrl") == true
                || match?.ConfigurationMetadata?.ContainsKey("webhookUrl") == true,
            DisplayName = match?.DisplayName,
            MaskedToken = match is null ? null : "********************",
            ConnectionId = match?.Id
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Configuraci?n del canal {provider} obtenida.", Data = response });
    }

    // GET /api/channels/{provider}/statistics ? sin m?tricas inventadas
    [HttpGet("{provider}/statistics")]
    public async Task<IActionResult> GetProviderStatistics(string provider, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Contexto de Tenant no v?lido.", Data = null });
        }

        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var match = connections.FirstOrDefault(c =>
            string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));

        var response = new
        {
            Provider = provider,
            ConnectionConfigured = match is not null,
            Enabled = match?.Enabled ?? false,
            CreatedAtUtc = match?.CreatedAtUtc,
            UpdatedAtUtc = match?.UpdatedAtUtc,
            Note = "Las m?tricas de mensajes requieren telemetr?a de canal persistida; no se inventan contadores."
        };

        return Ok(new ApiResponse<object> { Success = true, Message = $"Estad?sticas del canal {provider} obtenidas.", Data = response });
    }
}
