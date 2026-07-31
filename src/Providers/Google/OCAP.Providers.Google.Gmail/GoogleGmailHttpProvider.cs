using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Gmail;

public sealed class GoogleGmailHttpProvider : IEmailProvider
{
    private const string BaseUrl = "https://gmail.googleapis.com/gmail/v1/users/me";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<GoogleWorkspaceOptions> _options;

    public GoogleGmailHttpProvider(HttpClient httpClient, IOptionsMonitor<GoogleWorkspaceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<EmailMessage> SendEmailAsync(
        EmailMessage email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(email.To);

        var mime = string.Join("\r\n",
            $"To: {SanitizeHeader(email.To)}",
            string.IsNullOrWhiteSpace(email.From) ? null : $"From: {SanitizeHeader(email.From)}",
            $"Subject: {SanitizeHeader(email.Subject)}",
            "MIME-Version: 1.0",
            "Content-Type: text/plain; charset=utf-8",
            "Content-Transfer-Encoding: base64",
            string.Empty,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(email.Body ?? string.Empty)));

        using var request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/messages/send",
            JsonContent.Create(new { raw = ToBase64Url(Encoding.UTF8.GetBytes(mime)) }, options: JsonOptions));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var sent = await response.Content.ReadFromJsonAsync<GmailMessage>(JsonOptions, cancellationToken);
        if (sent is null || string.IsNullOrWhiteSpace(sent.Id))
        {
            throw new InvalidOperationException("Gmail devolvió una respuesta vacía o sin identificador.");
        }

        email.Id = sent.Id;
        email.SentAt = DateTime.UtcNow;
        return email;
    }

    public async Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults debe ser mayor que cero.");
        }

        using var listRequest = CreateRequest(
            HttpMethod.Get,
            $"{BaseUrl}/messages?maxResults={Math.Min(maxResults, 500)}");
        using var listResponse = await _httpClient.SendAsync(listRequest, cancellationToken);
        await EnsureSuccessAsync(listResponse, cancellationToken);
        var list = await listResponse.Content.ReadFromJsonAsync<GmailMessageList>(JsonOptions, cancellationToken);

        var result = new List<EmailMessage>();
        foreach (var item in list?.Messages ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            using var getRequest = CreateRequest(
                HttpMethod.Get,
                $"{BaseUrl}/messages/{Uri.EscapeDataString(item.Id)}?format=full");
            using var getResponse = await _httpClient.SendAsync(getRequest, cancellationToken);
            await EnsureSuccessAsync(getResponse, cancellationToken);
            var message = await getResponse.Content.ReadFromJsonAsync<GmailMessage>(JsonOptions, cancellationToken);
            if (message is not null)
            {
                result.Add(ToEmailMessage(message));
            }
        }

        return result;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var accessToken = _options.CurrentValue.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "Google Workspace no está configurado. Configure Google:AccessToken antes de usar Gmail.");
        }

        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static EmailMessage ToEmailMessage(GmailMessage message)
    {
        var headers = message.Payload?.Headers ?? [];
        return new EmailMessage
        {
            Id = message.Id ?? string.Empty,
            To = Header(headers, "To"),
            From = Header(headers, "From"),
            Subject = Header(headers, "Subject"),
            Body = ExtractBody(message.Payload),
            SentAt = long.TryParse(message.InternalDate, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)
                ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime
                : DateTime.UtcNow
        };
    }

    private static string Header(IEnumerable<GmailHeader> headers, string name) =>
        headers.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value
        ?? string.Empty;

    private static string ExtractBody(GmailPayload? payload)
    {
        if (!string.IsNullOrWhiteSpace(payload?.Body?.Data))
        {
            return Encoding.UTF8.GetString(FromBase64Url(payload.Body.Data));
        }

        var plainText = payload?.Parts?.FirstOrDefault(
            p => string.Equals(p.MimeType, "text/plain", StringComparison.OrdinalIgnoreCase));
        if (plainText is not null)
        {
            return ExtractBody(plainText);
        }

        foreach (var part in payload?.Parts ?? [])
        {
            var nested = ExtractBody(part);
            if (!string.IsNullOrEmpty(nested))
            {
                return nested;
            }
        }

        return string.Empty;
    }

    private static string SanitizeHeader(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
             .Replace("\n", string.Empty, StringComparison.Ordinal);

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
        return Convert.FromBase64String(base64);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Gmail API respondió {(int)response.StatusCode} ({response.ReasonPhrase}): {body}",
            null,
            response.StatusCode);
    }

    private sealed record GmailMessageList(List<GmailMessage>? Messages);
    private sealed record GmailMessage(string? Id, string? InternalDate, GmailPayload? Payload);
    private sealed record GmailPayload(
        string? MimeType,
        List<GmailHeader>? Headers,
        GmailBody? Body,
        List<GmailPayload>? Parts);
    private sealed record GmailHeader(string Name, string Value);
    private sealed record GmailBody(string? Data);
}
