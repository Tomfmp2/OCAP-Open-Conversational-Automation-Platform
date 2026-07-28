using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Telegram.Configuration;

namespace OCAP.Channels.Telegram.Services;

// Servicio en segundo plano para registrar automáticamente la URL del webhook en Telegram Bot API al iniciar la aplicación.
public class TelegramWebhookRegistrationService : IHostedService
{
    private readonly TelegramApiClient _apiClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramWebhookRegistrationService> _logger;

    public TelegramWebhookRegistrationService(
        TelegramApiClient apiClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramWebhookRegistrationService> logger)
    {
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogInformation("Registrando Webhook de Telegram en {WebhookUrl}...", _options.WebhookUrl);
            var success = await _apiClient.SetWebhookAsync(_options.WebhookUrl, _options.SecretToken, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Webhook de Telegram registrado exitosamente.");
            }
            else
            {
                _logger.LogWarning("No se pudo registrar el Webhook de Telegram durante el arranque.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
