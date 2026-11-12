using System.Net;
using System.Text.RegularExpressions;

namespace OCAP.Intelligence.Abstractions;

/// <summary>
/// Fallo de un proveedor de IA. <see cref="Exception.Message"/> llega al panel, así que nunca lleva la respuesta cruda
/// del proveedor; esa queda en <see cref="ProviderDetail"/> y solo sale en los registros.
/// </summary>
public sealed partial class AiProviderException : InvalidOperationException
{
    private const int MaxDetailLength = 2000;

    public string Provider { get; }
    public string Model { get; }
    public HttpStatusCode StatusCode { get; }
    public TimeSpan? RetryAfter { get; }
    public string ProviderDetail { get; }

    public AiProviderException(string provider, string model, HttpStatusCode statusCode, string? providerDetail, TimeSpan? retryAfter = null)
        : base(FriendlyMessage(provider, model, statusCode))
    {
        Provider = provider;
        Model = model;
        StatusCode = statusCode;
        RetryAfter = retryAfter;
        var detail = providerDetail ?? string.Empty;
        ProviderDetail = detail.Length > MaxDetailLength ? detail[..MaxDetailLength] : detail;
    }

    public bool IsQuotaExceeded => StatusCode == HttpStatusCode.TooManyRequests;

    /// <summary>Otro modelo podría atender la petición; un 400 fallaría igual, porque el problema es la petición.</summary>
    public bool OtherModelMayWork => (int)StatusCode >= 500 || StatusCode is HttpStatusCode.TooManyRequests
        or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.RequestTimeout;

    public static AiProviderException FromResponse(string provider, string model, HttpResponseMessage response, string? body)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new AiProviderException(provider, model, response.StatusCode, body, ReadRetryAfter(response, body));
    }

    public override string ToString() => $"{base.ToString()}{Environment.NewLine}Respuesta del proveedor: {ProviderDetail}";

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response, string? body)
    {
        var header = response.Headers.RetryAfter;
        if (header?.Delta is { } delta) return delta;
        if (header?.Date is { } date) return date - DateTimeOffset.UtcNow;

        // Gemini lo indica en el cuerpo: "retryDelay": "58964s".
        var match = RetryDelay().Match(body ?? string.Empty);
        return match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    private static string FriendlyMessage(string provider, string model, HttpStatusCode status) => status switch
    {
        HttpStatusCode.TooManyRequests => $"{provider} ({model}) agotó su cuota. Espera a que se renueve o asigna otro modelo en Modelos de IA.",
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => $"{provider} rechazó la API key. Revísala en Modelos de IA.",
        HttpStatusCode.NotFound => $"El modelo {model} no está disponible en {provider}. Elige otro en Modelos de IA.",
        >= HttpStatusCode.InternalServerError => $"{provider} no está disponible en este momento. Inténtalo de nuevo en unos minutos.",
        _ => $"{provider} no pudo procesar la solicitud (error {(int)status}).",
    };

    [GeneratedRegex("\"retryDelay\"\\s*:\\s*\"(\\d+(?:\\.\\d+)?)s\"")]
    private static partial Regex RetryDelay();
}
