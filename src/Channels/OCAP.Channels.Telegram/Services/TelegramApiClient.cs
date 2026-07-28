using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Telegram.Configuration;
using OCAP.Channels.Telegram.DTOs;

namespace OCAP.Channels.Telegram.Services;

// Cliente de bajo nivel para comunicación HTTP con Telegram Bot API.
public class TelegramApiClient
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramApiClient> _logger;

    public TelegramApiClient(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    // Envía un mensaje de texto a través del endpoint sendMessage de Telegram.
    public async Task<bool> SendMessageAsync(TelegramSendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogError("No se ha configurado un BotToken para TelegramApiClient.");
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<TelegramApiResponse<TelegramMessage>>(cancellationToken: cancellationToken);
                return apiResult?.Ok ?? false;
            }

            _logger.LogWarning("Respuesta no exitosa ({StatusCode}) al enviar mensaje a Telegram para el ChatId {ChatId}.", response.StatusCode, request.ChatId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de red o comunicación con Telegram Bot API para ChatId {ChatId}.", request.ChatId);
            return false;
        }
    }

    // Registra la URL del Webhook y el token de secreto opcional en Telegram API.
    public async Task<bool> SetWebhookAsync(string webhookUrl, string? secretToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/setWebhook";
            var payload = new
            {
                url = webhookUrl,
                secret_token = secretToken
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar Webhook en Telegram API.");
            return false;
        }
    }

    // Consulta los metadatos del Bot mediante el endpoint getMe para verificar conectividad y validez del token.
    public async Task<bool> GetMeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/getMe";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar credenciales con Telegram getMe.");
            return false;
        }
    }
}
