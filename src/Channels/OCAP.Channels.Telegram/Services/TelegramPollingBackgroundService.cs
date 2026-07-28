using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Telegram.Services;

public class TelegramPollingBackgroundService : BackgroundService
{
    private readonly TelegramApiClient _apiClient;
    private readonly TelegramMessageReceiver _receiver;
    private readonly ILogger<TelegramPollingBackgroundService> _logger;
    private long _lastOffset = 0;

    public TelegramPollingBackgroundService(
        TelegramApiClient apiClient,
        TelegramMessageReceiver receiver,
        ILogger<TelegramPollingBackgroundService> logger)
    {
        _apiClient = apiClient;
        _receiver = receiver;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio en segundo plano de Polling para Telegram iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _apiClient.GetUpdatesAsync(_lastOffset, 100, null, stoppingToken);
                foreach (var update in updates)
                {
                    _lastOffset = Math.Max(_lastOffset, update.UpdateId + 1);

                    var inboundMessage = TelegramWebhookMapper.ToIncomingMessage(update);
                    if (inboundMessage != null)
                    {
                        _logger.LogInformation("Procesando actualización Polling {UpdateId} de usuario Telegram {User}.", update.UpdateId, inboundMessage.ExternalUserId);
                        await _receiver.ReceiveMessageAsync(inboundMessage, stoppingToken);
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
