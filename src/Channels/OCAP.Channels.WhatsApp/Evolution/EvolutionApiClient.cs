using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Evolution;

public sealed class EvolutionQrResult
{
    public string InstanceName { get; set; } = string.Empty;
    public string? Base64 { get; set; }
    public string? Code { get; set; }
    public string? PairingCode { get; set; }
    public string? Status { get; set; }
    public string? RawJson { get; set; }
}

public sealed class EvolutionConnectionState
{
    public string InstanceName { get; set; } = string.Empty;
    public string State { get; set; } = "unknown";
    public bool IsOpen =>
        string.Equals(State, "open", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(State, "connected", StringComparison.OrdinalIgnoreCase);
}

public class EvolutionApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        }

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("apikey");
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
        }
    }

    public async Task<bool> CreateInstanceAsync(string instanceName, string? webhookUrl, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["instanceName"] = instanceName,
            ["qrcode"] = true,
            ["integration"] = "WHATSAPP-BAILEYS"
        };

        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            body["webhook"] = new
            {
                url = webhookUrl,
                byEvents = false,
                base64 = true,
                events = new[] { "MESSAGES_UPSERT", "CONNECTION_UPDATE" }
            };
        }

        using var response = await _httpClient.PostAsJsonAsync("instance/create", body, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var err = await response.Content.ReadAsStringAsync(cancellationToken);
        // Instancia ya existente suele devolver 403/409 — continuar a pedir QR.
        if ((int)response.StatusCode is 403 or 409 or 400)
        {
            _logger.LogInformation("Evolution create instance respondió {Status}: {Body}. Se intentará conectar igual.", response.StatusCode, err);
            return true;
        }

        _logger.LogWarning("No se pudo crear instancia Evolution {Instance}: {Status} {Body}", instanceName, response.StatusCode, err);
        return false;
    }

    public async Task<EvolutionQrResult> GetQrCodeAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"instance/connect/{Uri.EscapeDataString(instanceName)}", cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = new EvolutionQrResult
        {
            InstanceName = instanceName,
            RawJson = json,
            Status = response.IsSuccessStatusCode ? "ok" : response.StatusCode.ToString()
        };

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Evolution connect QR falló ({Status}): {Body}", response.StatusCode, json);
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("base64", out var b64))
            {
                result.Base64 = b64.GetString();
            }
            else if (root.TryGetProperty("qrcode", out var qr) && qr.ValueKind == JsonValueKind.Object)
            {
                if (qr.TryGetProperty("base64", out var qb)) result.Base64 = qb.GetString();
                if (qr.TryGetProperty("code", out var qc)) result.Code = qc.GetString();
            }

            if (root.TryGetProperty("code", out var code))
            {
                result.Code = code.GetString();
            }

            if (root.TryGetProperty("pairingCode", out var pair))
            {
                result.PairingCode = pair.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo parsear QR de Evolution.");
        }

        return result;
    }

    public async Task<EvolutionConnectionState> GetConnectionStateAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"instance/connectionState/{Uri.EscapeDataString(instanceName)}",
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var state = new EvolutionConnectionState { InstanceName = instanceName, State = "unknown" };

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Evolution connectionState falló ({Status}): {Body}", response.StatusCode, json);
            state.State = "error";
            return state;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("instance", out var inst) &&
                inst.TryGetProperty("state", out var st))
            {
                state.State = st.GetString() ?? "unknown";
            }
            else if (root.TryGetProperty("state", out var st2))
            {
                state.State = st2.GetString() ?? "unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo parsear connectionState Evolution.");
        }

        return state;
    }

    public async Task<bool> SendTextAsync(string instanceName, string number, string text, CancellationToken cancellationToken = default)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        var body = new
        {
            number = digits,
            textMessage = new { text },
            text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"message/sendText/{Uri.EscapeDataString(instanceName)}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            request.Headers.TryAddWithoutValidation("apikey", _settings.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var err = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Evolution sendText falló ({Status}): {Body}", response.StatusCode, err);
        return false;
    }

    public async Task<bool> DeleteInstanceAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"instance/delete/{Uri.EscapeDataString(instanceName)}",
            cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
    }
}
