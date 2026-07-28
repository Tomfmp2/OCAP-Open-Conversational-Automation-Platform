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

    // Consulta metadatos de un bot mediante getMe pasando un botToken explícito.
    public async Task<TelegramBotInfoDto?> GetMeWithTokenAsync(string botToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken)) return null;

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/getMe";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var apiResult = await response.Content.ReadFromJsonAsync<TelegramApiResponse<TelegramUser>>(cancellationToken: cancellationToken);
            if (apiResult?.Ok == true && apiResult.Result != null)
            {
                return new TelegramBotInfoDto
                {
                    Id = apiResult.Result.Id,
                    IsBot = apiResult.Result.IsBot,
                    FirstName = apiResult.Result.FirstName,
                    Username = apiResult.Result.Username,
                    CanJoinGroups = true,
                    CanReadExtraMessages = true
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar Telegram getMe con el token especificado.");
            return null;
        }
    }

    // Envía un mensaje de texto a través del endpoint sendMessage de Telegram.
    public async Task<bool> SendMessageAsync(TelegramSendMessageRequest request, string? overrideToken = null, CancellationToken cancellationToken = default)
    {
        var token = !string.IsNullOrWhiteSpace(overrideToken) ? overrideToken : _options.BotToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("No se ha configurado un BotToken para TelegramApiClient.");
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{token}/sendMessage";
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
    public async Task<bool> SetWebhookAsync(string webhookUrl, string? secretToken, string? overrideToken = null, CancellationToken cancellationToken = default)
    {
        var token = !string.IsNullOrWhiteSpace(overrideToken) ? overrideToken : _options.BotToken;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var url = $"https://api.telegram.org/bot{token}/setWebhook";
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

    // Elimina la URL del Webhook en Telegram API.
    public async Task<bool> DeleteWebhookAsync(string? overrideToken = null, CancellationToken cancellationToken = default)
    {
        var token = !string.IsNullOrWhiteSpace(overrideToken) ? overrideToken : _options.BotToken;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var url = $"https://api.telegram.org/bot{token}/deleteWebhook";
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar Webhook en Telegram API.");
            return false;
        }
    }

    // Obtiene actualizaciones en modo Polling (getUpdates).
    public async Task<List<TelegramUpdate>> GetUpdatesAsync(long offset = 0, int limit = 100, string? overrideToken = null, CancellationToken cancellationToken = default)
    {
        var token = !string.IsNullOrWhiteSpace(overrideToken) ? overrideToken : _options.BotToken;
        if (string.IsNullOrWhiteSpace(token)) return new List<TelegramUpdate>();

        try
        {
            var url = $"https://api.telegram.org/bot{token}/getUpdates?offset={offset}&limit={limit}&timeout=5";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return new List<TelegramUpdate>();

            var apiResult = await response.Content.ReadFromJsonAsync<TelegramApiResponse<List<TelegramUpdate>>>(cancellationToken: cancellationToken);
            return apiResult?.Result ?? new List<TelegramUpdate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar getUpdates de Telegram en modo Polling.");
            return new List<TelegramUpdate>();
        }
    }

    // Consulta los metadatos del Bot mediante el endpoint getMe para verificar conectividad y validez del token.
    public async Task<bool> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var botInfo = await GetMeWithTokenAsync(_options.BotToken, cancellationToken);
        return botInfo != null;
    }
}
