using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Channels.WhatsApp.Services;

public interface IWhatsAppRuntimeManager
{
    Task<bool> ValidateTokenAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default);
    Task<ChannelConnection> RegisterConnectionAsync(Guid tenantId, string displayName, string phoneNumberId, string token, CancellationToken cancellationToken = default);
    Task<WhatsAppHealthResultDto> HealthCheckAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default);
    Task<bool> ReconnectAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);
}

public class WhatsAppHealthResultDto
{
    public bool IsHealthy { get; set; }
    public long LatencyMs { get; set; }
    public string? PhoneNumberId { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

public class WhatsAppRuntimeManager : IWhatsAppRuntimeManager
{
    private readonly WhatsAppApiClient _apiClient;
    private readonly IChannelConnectionManager _connectionManager;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WhatsAppRuntimeManager> _logger;

    public WhatsAppRuntimeManager(
        WhatsAppApiClient apiClient,
        IChannelConnectionManager connectionManager,
        ISecurityAuditService auditService,
        ILogger<WhatsAppRuntimeManager> logger)
    {
        _apiClient = apiClient;
        _connectionManager = connectionManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<bool> ValidateTokenAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default)
    {
        return await _apiClient.ValidatePhoneNumberAsync(phoneNumberId, token, cancellationToken);
    }

    public async Task<ChannelConnection> RegisterConnectionAsync(
        Guid tenantId,
        string displayName,
        string phoneNumberId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var isValid = await ValidateTokenAsync(phoneNumberId, token, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException("El Token o PhoneNumberId proporcionado es inválido o no se pudo verificar con WhatsApp Cloud API.");
        }

        var metadata = new Dictionary<string, string>
        {
            ["PhoneNumberId"] = phoneNumberId,
            ["ConnectionMode"] = "webhook"
        };

        var connection = await _connectionManager.CreateConnectionAsync(
            tenantId,
            "WhatsApp",
            displayName,
            token,
            metadata,
            cancellationToken);

        _logger.LogInformation("Conexión de WhatsApp Cloud API registrada exitosamente para Tenant {TenantId} con PhoneNumberId {PhoneNumberId} (ID: {ConnectionId}).",
            tenantId, phoneNumberId, connection.Id);

        return connection;
    }

    public async Task<WhatsAppHealthResultDto> HealthCheckAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var isValid = await _apiClient.ValidatePhoneNumberAsync(phoneNumberId, token, cancellationToken);
        sw.Stop();

        if (isValid)
        {
            return new WhatsAppHealthResultDto
            {
                IsHealthy = true,
                LatencyMs = sw.ElapsedMilliseconds,
                PhoneNumberId = phoneNumberId,
                StatusMessage = $"Conexión activa con WhatsApp Cloud API (PhoneNumberId: {phoneNumberId})."
            };
        }

        return new WhatsAppHealthResultDto
        {
            IsHealthy = false,
            LatencyMs = sw.ElapsedMilliseconds,
            PhoneNumberId = phoneNumberId,
            StatusMessage = "Falla de conectividad o credenciales inválidas con WhatsApp Cloud API."
        };
    }

    public async Task<bool> ReconnectAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null) return false;

        _logger.LogInformation("Reconectando conexión de WhatsApp (ID: {ConnectionId}) para Tenant {TenantId}...", connectionId, tenantId);

        await _connectionManager.EnableChannelAsync(tenantId, connectionId, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var removed = await _connectionManager.RemoveConnectionAsync(tenantId, connectionId, cancellationToken);
        if (removed)
        {
            _logger.LogInformation("Conexión de WhatsApp (ID: {ConnectionId}) eliminada exitosamente para Tenant {TenantId}.", connectionId, tenantId);
        }

        return removed;
    }
}
