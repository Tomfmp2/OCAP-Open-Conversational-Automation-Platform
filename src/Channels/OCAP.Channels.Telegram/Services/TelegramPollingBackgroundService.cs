using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Core.Events.Distributed;
using OCAP.Security.Abstractions;

namespace OCAP.Channels.Telegram.Services;

/// <summary>
/// Consulta <c>getUpdates</c> de cada bot registrado desde el panel en modo polling, con su propio token, y responde
/// cada mensaje con el asistente de su organización. El offset de cada bot se guarda en su conexión: confirma a
/// Telegram lo ya procesado y sobrevive a los reinicios.
/// </summary>
public class TelegramPollingBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);
    private const string OffsetKey = "PollingOffset";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramPollingBackgroundService> _logger;

    public TelegramPollingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramPollingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Polling de Telegram iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el ciclo de polling de Telegram.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<(Guid ConnectionId, Guid TenantId, string Token, long Offset)> bots;
        using (var scope = _scopeFactory.CreateScope())
        {
            var connections = scope.ServiceProvider.GetRequiredService<IChannelConnectionManager>();
            var polling = (await connections.GetEnabledConnectionsByProviderAsync("Telegram", cancellationToken))
                .Where(c => !c.ConfigurationMetadata.TryGetValue("ConnectionMode", out var mode)
                            || !string.Equals(mode, "webhook", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var resolved = new List<(Guid, Guid, string, long)>();
            foreach (var connection in polling)
            {
                var token = await connections.GetCredentialsAsync(connection, cancellationToken);
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("El bot de Telegram {ConnectionId} no tiene un token legible: no se consultan sus mensajes.", connection.Id);
                    continue;
                }

                long.TryParse(connection.ConfigurationMetadata.GetValueOrDefault(OffsetKey), out var offset);
                resolved.Add((connection.Id, connection.TenantId, token, offset));
            }
            bots = resolved;
        }

        foreach (var bot in bots)
        {
            await PollBotAsync(bot.ConnectionId, bot.TenantId, bot.Token, bot.Offset, cancellationToken);
        }
    }

    private async Task PollBotAsync(Guid connectionId, Guid tenantId, string token, long offset, CancellationToken cancellationToken)
    {
        List<DTOs.TelegramUpdate> updates;
        using (var scope = _scopeFactory.CreateScope())
        {
            var apiClient = scope.ServiceProvider.GetRequiredService<TelegramApiClient>();
            updates = await apiClient.GetUpdatesAsync(offset, 100, token, cancellationToken);
        }
        if (updates.Count == 0) return;

        foreach (var update in updates)
        {
            // Un scope por mensaje, con la organización del bot: sus lecturas y escrituras quedan aisladas.
            using var scope = _scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantScope>().Use(tenantId);

            // Misma clave que el webhook: un update ya atendido (por webhook, otra réplica o antes de un reinicio) no se
            // responde dos veces.
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            if (!await inbox.TryClaimAsync($"telegram-update:{connectionId:N}:{update.UpdateId}", ChannelInbox.ConsumerGroup, cancellationToken))
                continue;

            var processor = scope.ServiceProvider.GetRequiredService<TelegramConversationProcessor>();
            try
            {
                await processor.ProcessAsync(tenantId, connectionId, token, update, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudo procesar el update {UpdateId} del bot {ConnectionId}.", update.UpdateId, connectionId);
            }
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ITenantScope>().Use(tenantId);
            var connections = scope.ServiceProvider.GetRequiredService<IChannelConnectionManager>();
            var next = updates.Max(u => u.UpdateId) + 1;
            await connections.SetMetadataValueAsync(tenantId, connectionId, OffsetKey, next.ToString(), cancellationToken);
        }
    }
}
