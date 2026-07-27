using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Evolution;

// Cliente HTTP profesional para comunicarse con la instancia de Evolution API.
// Utiliza HttpClientFactory para una gestión eficiente de conexiones sin socket exhaustion.
public class EvolutionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<EvolutionApiClient> _logger;

    public EvolutionApiClient(
        HttpClient httpClient,
        IOptions<WhatsAppSettings> settings,
        ILogger<EvolutionApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    // Envía un mensaje de texto hacia un número o JID a través de Evolution API.
    public async Task<bool> SendTextMessageAsync(string number, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.Instance))
        {
            _logger.LogError("Imposible enviar mensaje WhatsApp: BaseUrl o Instance no están configuradas.");
            return false;
        }

        // Construir la URL completa del endpoint de envío de Evolution API.
        var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/message/sendText/{_settings.Instance}";

        // Estructurar el cuerpo de la petición según la especificación de Evolution API.
        var payload = new
        {
            number = FormatPhoneNumber(number),
            text = message
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            
            // Agregar API Key en los headers si está configurada.
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                request.Headers.Add("apikey", _settings.ApiKey);
            }

            request.Content = JsonContent.Create(payload);

            _logger.LogInformation("Enviando petición a Evolution API para {Number} en instancia {Instance}", number, _settings.Instance);
            
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Mensaje despachado exitosamente vía Evolution API a {Number}", number);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error al enviar mensaje vía Evolution API. StatusCode: {StatusCode}, Detalle: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Excepción de red al comunicarse con Evolution API en {Url}", requestUrl);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout o cancelación al enviar mensaje vía Evolution API");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no esperado en cliente Evolution API");
            return false;
        }
    }

    // Formatea el número de teléfono removiendo sufijos JID o caracteres no numéricos.
    private static string FormatPhoneNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number)) return string.Empty;
        var clean = number.Split('@')[0];
        return new string(clean.Where(char.IsDigit).ToArray());
    }
}
