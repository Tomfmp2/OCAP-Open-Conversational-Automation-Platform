using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Telegram.Services;

public class TelegramPollingBackgroundService : BackgroundService
{
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
        _logger.LogInformation("Servicio en segundo plano de Polling para Telegram iniciado.");
        long lastOffset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var apiClient = scope.ServiceProvider.GetRequiredService<TelegramApiClient>();
                var receiver = scope.ServiceProvider.GetRequiredService<TelegramMessageReceiver>();

                var updates = await apiClient.GetUpdatesAsync(lastOffset, 100, null, stoppingToken);
                foreach (var update in updates)
                {
                    lastOffset = Math.Max(lastOffset, update.UpdateId + 1);

                    var inboundMessage = TelegramWebhookMapper.ToIncomingMessage(update);
                    if (inboundMessage != null)
                    {
                        _logger.LogInformation(
                            "Procesando actualización Polling {UpdateId} de usuario Telegram {User}.",
                            update.UpdateId,
                            inboundMessage.ExternalUserId);
                        await receiver.ReceiveMessageAsync(inboundMessage, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el ciclo de Polling de Telegram.");
            }

            await Task.Delay(2000, stoppingToken);
        }

        _logger.LogInformation("Servicio de Polling para Telegram detenido.");
    }
}
