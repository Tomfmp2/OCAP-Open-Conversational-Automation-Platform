using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Channels.Telegram.Services;

public interface ITelegramBotRuntimeManager
{
    Task<TelegramBotInfoDto?> ValidateTokenAsync(string botToken, CancellationToken cancellationToken = default);
    Task<ChannelConnection> RegisterBotAsync(Guid tenantId, string displayName, string botToken, string connectionMode, string? webhookBaseUrl, CancellationToken cancellationToken = default);
    Task<TelegramHealthResultDto> HealthCheckAsync(string botToken, CancellationToken cancellationToken = default);
    Task<bool> ReconnectAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteBotAsync(Guid tenantId, Guid connectionId, string botToken, CancellationToken cancellationToken = default);
}

public class TelegramHealthResultDto
{
    public bool IsHealthy { get; set; }
    public long LatencyMs { get; set; }
    public string? BotUsername { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

public class TelegramBotRuntimeManager : ITelegramBotRuntimeManager
{
    private readonly TelegramApiClient _apiClient;
    private readonly IChannelConnectionManager _connectionManager;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<TelegramBotRuntimeManager> _logger;

    public TelegramBotRuntimeManager(
        TelegramApiClient apiClient,
        IChannelConnectionManager connectionManager,
        ISecurityAuditService auditService,
        ILogger<TelegramBotRuntimeManager> logger)
    {
        _apiClient = apiClient;
        _connectionManager = connectionManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TelegramBotInfoDto?> ValidateTokenAsync(string botToken, CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetMeWithTokenAsync(botToken, cancellationToken);
    }

    public async Task<ChannelConnection> RegisterBotAsync(
        Guid tenantId,
        string displayName,
        string botToken,
        string connectionMode,
        string? webhookBaseUrl,
        CancellationToken cancellationToken = default)
    {
        // 1. Validar el token con Telegram API getMe
        var botInfo = await ValidateTokenAsync(botToken, cancellationToken);
        if (botInfo == null)
        {
            throw new InvalidOperationException("El Bot Token proporcionado es inválido o no se pudo verificar con Telegram API.");
        }

        // 2. Registrar Webhook si mode == "webhook"
        string? configuredWebhookUrl = null;
        if (string.Equals(connectionMode, "webhook", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(webhookBaseUrl))
        {
            configuredWebhookUrl = $"{webhookBaseUrl.TrimEnd('/')}/api/channels/telegram/webhook";
            var secretToken = Guid.NewGuid().ToString("N");
            var webhookSuccess = await _apiClient.SetWebhookAsync(configuredWebhookUrl, secretToken, botToken, cancellationToken);
            if (!webhookSuccess)
            {
                _logger.LogWarning("No se pudo registrar la URL de Webhook {Url} en Telegram API.", configuredWebhookUrl);
            }
        }

        // 3. Crear metadata de configuración
        var metadata = new Dictionary<string, string>
        {
            ["BotId"] = botInfo.Id.ToString(),
            ["BotUsername"] = botInfo.Username ?? botInfo.FirstName,
            ["ConnectionMode"] = connectionMode.ToLowerInvariant(),
            ["WebhookUrl"] = configuredWebhookUrl ?? "polling"
        };

        // 4. Crear la conexión en el gestor multi-tenant de OCAP
        var connection = await _connectionManager.CreateConnectionAsync(
            tenantId,
            "Telegram",
            displayName,
            botToken,
            metadata,
            cancellationToken);

        _logger.LogInformation("Bot de Telegram registrado exitosamente para Tenant {TenantId} con Username @{Username} (ID: {ConnectionId}).",
            tenantId, botInfo.Username, connection.Id);

        return connection;
    }

    public async Task<TelegramHealthResultDto> HealthCheckAsync(string botToken, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var botInfo = await _apiClient.GetMeWithTokenAsync(botToken, cancellationToken);
        sw.Stop();

        if (botInfo != null)
        {
            return new TelegramHealthResultDto
            {
                IsHealthy = true,
                LatencyMs = sw.ElapsedMilliseconds,
                BotUsername = botInfo.Username,
                StatusMessage = $"Conexión activa con Telegram API (@{botInfo.Username})."
            };
        }

        return new TelegramHealthResultDto
        {
            IsHealthy = false,
            LatencyMs = sw.ElapsedMilliseconds,
            BotUsername = null,
            StatusMessage = "Falla de conectividad o credenciales inválidas con Telegram API."
        };
    }

    public async Task<bool> ReconnectAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null) return false;

        _logger.LogInformation("Reconectando bot de Telegram (ID: {ConnectionId}) para Tenant {TenantId}...", connectionId, tenantId);

        // Forzar reactivación
        await _connectionManager.EnableChannelAsync(tenantId, connectionId, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteBotAsync(Guid tenantId, Guid connectionId, string botToken, CancellationToken cancellationToken = default)
    {
        // 1. Desregistrar Webhook en Telegram API
        if (!string.IsNullOrWhiteSpace(botToken))
        {
            await _apiClient.DeleteWebhookAsync(botToken, cancellationToken);
        }

        // 2. Eliminar conexión del Tenant
        var removed = await _connectionManager.RemoveConnectionAsync(tenantId, connectionId, cancellationToken);
        if (removed)
        {
            _logger.LogInformation("Bot de Telegram (ID: {ConnectionId}) eliminado exitosamente para Tenant {TenantId}.", connectionId, tenantId);
        }

        return removed;
    }
}
