using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Evolution;

/// <summary>
/// Cliente de Evolution API v2 (contrato de la v2.3.7). Todas las llamadas usan la API key global en la cabecera
/// <c>apikey</c>. Los errores se lanzan como <see cref="EvolutionApiException"/>: nunca se da por buena una
/// respuesta fallida.
/// </summary>
public class EvolutionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EvolutionApiClient> _logger;

    public EvolutionApiClient(HttpClient httpClient, IOptions<WhatsAppSettings> settings, ILogger<EvolutionApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var options = settings.Value;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            _httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("apikey");
            _httpClient.DefaultRequestHeaders.Add("apikey", options.ApiKey);
        }
    }

    /// <summary>Crea la instancia Baileys con su webhook ya configurado (eventos y cabeceras propias).</summary>
    public async Task CreateInstanceAsync(string instanceName, EvolutionWebhookConfig webhook, CancellationToken cancellationToken)
    {
        var body = new
        {
            instanceName,
            integration = "WHATSAPP-BAILEYS",
            qrcode = true,
            groupsIgnore = true,
            webhook = webhook.ToRequest()
        };
        using var response = await _httpClient.PostAsJsonAsync("instance/create", body, cancellationToken);
        await EnsureSuccessAsync(response, "instance/create", cancellationToken);
    }

    /// <summary>Reescribe el webhook de una instancia existente (p. ej. si cambió la URL o el secreto).</summary>
    public async Task SetWebhookAsync(string instanceName, EvolutionWebhookConfig webhook, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"webhook/set/{Uri.EscapeDataString(instanceName)}",
            new { webhook = webhook.ToRequest() },
            cancellationToken);
        await EnsureSuccessAsync(response, "webhook/set", cancellationToken);
    }

    /// <summary>Estado de la instancia; <see cref="EvolutionStates.Missing"/> si no existe.</summary>
    public async Task<string> GetConnectionStateAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"instance/connectionState/{Uri.EscapeDataString(instanceName)}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound) return EvolutionStates.Missing;
        var json = await ReadSuccessAsync(response, "instance/connectionState", cancellationToken);
        return ReadState(json) ?? EvolutionStates.Close;
    }

    /// <summary>Pide la conexión: si la instancia no está vinculada devuelve el QR; si ya lo está, solo el estado.</summary>
    public async Task<EvolutionConnectResult> ConnectAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"instance/connect/{Uri.EscapeDataString(instanceName)}", cancellationToken);
        var json = await ReadSuccessAsync(response, "instance/connect", cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Vinculada: Evolution responde el connectionState { instance: { state: "open" } }.
        var state = ReadState(json);
        if (state == EvolutionStates.Open) return new EvolutionConnectResult(EvolutionStates.Open, null);

        var qrElement = root.TryGetProperty("qrcode", out var nested) && nested.ValueKind == JsonValueKind.Object ? nested : root;
        var qr = new EvolutionQrCode(
            GetString(qrElement, "base64"),
            GetString(qrElement, "code"),
            GetString(qrElement, "pairingCode"));
        return new EvolutionConnectResult(EvolutionStates.Connecting, qr.Base64 is null && qr.Code is null ? null : qr);
    }

    public async Task LogoutAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"instance/logout/{Uri.EscapeDataString(instanceName)}", cancellationToken);
        // Desvincular una instancia que ya estaba cerrada no es un error para OCAP.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest) return;
        await EnsureSuccessAsync(response, "instance/logout", cancellationToken);
    }

    public async Task DeleteInstanceAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"instance/delete/{Uri.EscapeDataString(instanceName)}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound) return;
        await EnsureSuccessAsync(response, "instance/delete", cancellationToken);
    }

    /// <summary>Envía un texto. <paramref name="number"/> es el número en formato internacional, solo dígitos.</summary>
    public async Task SendTextAsync(string instanceName, string number, string text, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"message/sendText/{Uri.EscapeDataString(instanceName)}",
            new { number, text },
            cancellationToken);
        await EnsureSuccessAsync(response, "message/sendText", cancellationToken);
    }

    /// <summary>Comprueba que Evolution responde y acepta la API key.</summary>
    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("instance/fetchInstances", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Evolution API no alcanzable.");
            return false;
        }
    }

    private static string? ReadState(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return null;
        if (root.TryGetProperty("instance", out var instance) && instance.ValueKind == JsonValueKind.Object)
            return GetString(instance, "state");
        return GetString(root, "state");
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private async Task<string> ReadSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, operation, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Evolution {Operation} respondió {Status}: {Body}", operation, (int)response.StatusCode, body);
        throw new EvolutionApiException(operation, (int)response.StatusCode, body);
    }
}

/// <summary>Webhook por instancia: URL de OCAP, cabeceras de autenticación y eventos suscritos.</summary>
public sealed record EvolutionWebhookConfig(string Url, IReadOnlyDictionary<string, string> Headers)
{
    internal object ToRequest() => new
    {
        enabled = true,
        url = Url,
        headers = Headers,
        byEvents = false,
        base64 = true,
        events = EvolutionEvents.Subscribed
    };
}
