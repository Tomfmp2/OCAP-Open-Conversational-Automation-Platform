using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.DTOs;

namespace OCAP.Channels.WhatsApp.Services;

public class WhatsAppApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppApiClient> _logger;

    public WhatsAppApiClient(
        HttpClient httpClient,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendMessageAsync(
        string phoneNumberId, 
        WhatsAppCloudSendMessageRequest request, 
        string? overrideToken = null, 
        CancellationToken cancellationToken = default)
    {
        var token = !string.IsNullOrWhiteSpace(overrideToken) ? overrideToken : _settings.ApiToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("No se ha configurado un Token para WhatsAppApiClient.");
            return false;
        }

        try
        {
            var url = $"https://graph.facebook.com/v17.0/{phoneNumberId}/messages";
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            requestMessage.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<WhatsAppCloudApiResponse>(cancellationToken: cancellationToken);
                return apiResult?.Messages?.Any() == true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Respuesta no exitosa ({StatusCode}) al enviar mensaje a WhatsApp: {Error}", response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de red o comunicación con WhatsApp Cloud API para PhoneNumberId {PhoneNumberId}.", phoneNumberId);
            return false;
        }
    }

    public async Task<bool> ValidatePhoneNumberAsync(string phoneNumberId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneNumberId)) return false;

        try
        {
            var url = $"https://graph.facebook.com/v17.0/{phoneNumberId}";
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar PhoneNumberId {PhoneNumberId} con WhatsApp Cloud API.", phoneNumberId);
            return false;
        }
    }
}
