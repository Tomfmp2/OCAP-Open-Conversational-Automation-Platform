using System.Diagnostics;
using System.Text.RegularExpressions;
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
    Task<WhatsAppQrConnectResult> ConnectWithQrAsync(Guid tenantId, string displayName, string? instanceName, CancellationToken cancellationToken = default);
    Task<EvolutionQrResult> RefreshQrAsync(string instanceName, CancellationToken cancellationToken = default);
    Task<EvolutionConnectionState> GetEvolutionStateAsync(string instanceName, CancellationToken cancellationToken = default);
    Task<WhatsAppHealthResultDto> HealthCheckAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default);
    Task<WhatsAppHealthResultDto> HealthCheckEvolutionAsync(CancellationToken cancellationToken = default);
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

public class WhatsAppQrConnectResult
{
    public ChannelConnection Connection { get; set; } = null!;
    public EvolutionQrResult Qr { get; set; } = null!;
    public string InstanceName { get; set; } = string.Empty;
}

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

    public async Task<WhatsAppQrConnectResult> ConnectWithQrAsync(
        Guid tenantId,
        string displayName,
        string? instanceName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Evolution API no está configurada. Define WhatsApp__BaseUrl y WhatsApp__ApiKey (o EVOLUTION_API_URL / EVOLUTION_API_KEY).");
        }

        var safeName = string.IsNullOrWhiteSpace(instanceName)
            ? SanitizeInstanceName(displayName)
            : SanitizeInstanceName(instanceName!);

        var webhookUrl = !string.IsNullOrWhiteSpace(_settings.WebhookUrl)
            ? _settings.WebhookUrl
            : "http://host.docker.internal:5229/api/webhooks/whatsapp";

        var created = await _evolutionClient.CreateInstanceAsync(safeName, webhookUrl, cancellationToken);
        if (!created)
        {
            throw new InvalidOperationException("No se pudo crear la instancia en Evolution API. ¿Está corriendo en " + _settings.BaseUrl + "?");
        }

        var qr = await _evolutionClient.GetQrCodeAsync(safeName, cancellationToken);
        var secretRef = $"evolution:{safeName}:{_settings.ApiKey}";
        var metadata = new Dictionary<string, string>
        {
            ["ConnectionMode"] = "evolution",
            ["Instance"] = safeName,
            ["BaseUrl"] = _settings.BaseUrl
        };

        ChannelConnection connection;
        try
        {
            connection = await _connectionManager.CreateConnectionAsync(
                tenantId, "WhatsApp", displayName, secretRef, metadata, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Ya existe WhatsApp en el tenant: actualizar configuración
            var existing = (await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken))
                .FirstOrDefault(c => string.Equals(c.Provider, "WhatsApp", StringComparison.OrdinalIgnoreCase));
            if (existing == null) throw;

            connection = await _connectionManager.UpdateConfigurationAsync(
                tenantId, existing.Id, displayName, secretRef, metadata, cancellationToken)
                ?? existing;
        }

        _logger.LogInformation("WhatsApp Evolution QR iniciado. Instance={Instance} Tenant={TenantId}", safeName, tenantId);

        return new WhatsAppQrConnectResult
        {
            Connection = connection,
            Qr = qr,
            InstanceName = safeName
        };
    }

    public Task<EvolutionQrResult> RefreshQrAsync(string instanceName, CancellationToken cancellationToken = default)
        => _evolutionClient.GetQrCodeAsync(instanceName, cancellationToken);

    public Task<EvolutionConnectionState> GetEvolutionStateAsync(string instanceName, CancellationToken cancellationToken = default)
        => _evolutionClient.GetConnectionStateAsync(instanceName, cancellationToken);

    public async Task<WhatsAppHealthResultDto> HealthCheckAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default)
    {
        if (_settings.IsEvolution)
        {
            return await HealthCheckEvolutionAsync(cancellationToken);
        }

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

    public async Task<WhatsAppHealthResultDto> HealthCheckEvolutionAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var state = await _evolutionClient.GetConnectionStateAsync(_settings.Instance, cancellationToken);
            sw.Stop();
            return new WhatsAppHealthResultDto
            {
                IsHealthy = state.IsOpen || state.State is not ("error" or "unknown"),
                LatencyMs = sw.ElapsedMilliseconds,
                Provider = "Evolution",
                PhoneNumberId = _settings.Instance,
                StatusMessage = $"Evolution instance '{_settings.Instance}' state={state.State}"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new WhatsAppHealthResultDto
            {
                IsHealthy = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Provider = "Evolution",
                StatusMessage = $"Evolution no alcanzable en {_settings.BaseUrl}: {ex.Message}"
            };
        }
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
        if (connection?.ConfigurationMetadata.TryGetValue("Instance", out var instance) == true &&
            !string.IsNullOrWhiteSpace(instance))
        {
            await _evolutionClient.DeleteInstanceAsync(instance, cancellationToken);
        }

        return await _connectionManager.RemoveConnectionAsync(tenantId, connectionId, cancellationToken);
    }

    private static string SanitizeInstanceName(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug)) slug = "ocap";
        if (slug.Length > 32) slug = slug[..32];
        return $"ocap-{slug}";
    }
}
