using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;
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
    public string? Provider { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

/// <summary>
/// Conexiones de WhatsApp Cloud API (Meta) y borrado de conexiones. La vinculación por QR con Evolution la hace
/// <see cref="WhatsAppLinkService"/>.
/// </summary>
public class WhatsAppRuntimeManager : IWhatsAppRuntimeManager
{
    private readonly WhatsAppApiClient _apiClient;
    private readonly EvolutionApiClient _evolutionClient;
    private readonly IChannelConnectionManager _connectionManager;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppRuntimeManager> _logger;

    public WhatsAppRuntimeManager(
        WhatsAppApiClient apiClient,
        EvolutionApiClient evolutionClient,
        IChannelConnectionManager connectionManager,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppRuntimeManager> logger)
    {
        _apiClient = apiClient;
        _evolutionClient = evolutionClient;
        _connectionManager = connectionManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<bool> ValidateTokenAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default)
        => _apiClient.ValidatePhoneNumberAsync(phoneNumberId, token, cancellationToken);

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
            throw new InvalidOperationException("Token o PhoneNumberId inválido en WhatsApp Cloud API.");
        }

        var metadata = new Dictionary<string, string>
        {
            ["PhoneNumberId"] = phoneNumberId,
            ["ConnectionMode"] = "cloud"
        };

        return await _connectionManager.CreateConnectionAsync(
            tenantId, "WhatsApp", displayName, token, metadata, cancellationToken);
    }

    public async Task<WhatsAppHealthResultDto> HealthCheckAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var isValid = await _apiClient.ValidatePhoneNumberAsync(phoneNumberId, token, cancellationToken);
        sw.Stop();
        return new WhatsAppHealthResultDto
        {
            IsHealthy = isValid,
            LatencyMs = sw.ElapsedMilliseconds,
            PhoneNumberId = phoneNumberId,
            Provider = "Cloud",
            StatusMessage = isValid ? "WhatsApp Cloud API OK" : "Credenciales Cloud inválidas"
        };
    }

    public async Task<bool> ReconnectAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null) return false;
        await _connectionManager.EnableChannelAsync(tenantId, connectionId, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        // La metadata la puede escribir el cliente: la instancia se deriva del tenant para no tocar la de otro.
        if (connection?.ConfigurationMetadata.TryGetValue("ConnectionMode", out var mode) == true &&
            string.Equals(mode, "evolution", StringComparison.OrdinalIgnoreCase))
        {
            await _evolutionClient.DeleteInstanceAsync(EvolutionInstanceIdentity.InstanceNameFor(tenantId), cancellationToken);
        }

        return await _connectionManager.RemoveConnectionAsync(tenantId, connectionId, cancellationToken);
    }
}
