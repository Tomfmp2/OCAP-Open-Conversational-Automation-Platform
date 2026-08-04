using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Telegram.Configuration;

namespace OCAP.Channels.Telegram.Services;

// Servicio en segundo plano para registrar automáticamente la URL del webhook en Telegram Bot API al iniciar la aplicación.
public class TelegramWebhookRegistrationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<TelegramOptions> _options;
    private readonly ILogger<TelegramWebhookRegistrationService> _logger;

    public TelegramWebhookRegistrationService(
        IServiceScopeFactory scopeFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramWebhookRegistrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var webhookUrl = _options.Value.WebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        _logger.LogInformation("Registrando Webhook de Telegram en {WebhookUrl}...", webhookUrl);
        using var scope = _scopeFactory.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<TelegramApiClient>();
        var success = await apiClient.SetWebhookAsync(webhookUrl, _options.Value.SecretToken, cancellationToken: cancellationToken);
        if (success)
        {
            _logger.LogInformation("Webhook de Telegram registrado exitosamente.");
        }
        else
        {
            _logger.LogWarning("No se pudo registrar el Webhook de Telegram durante el arranque.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
