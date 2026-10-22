using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Sheets;

public sealed class GoogleSheetsHttpProvider : ISpreadsheetProvider
{
    private const string BaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IGoogleAccessTokenProvider _tokens;

    public GoogleSheetsHttpProvider(HttpClient httpClient, IGoogleAccessTokenProvider tokens)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    public async Task<bool> AppendRowAsync(
        SpreadsheetAppendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SheetName);

        var spreadsheetId = Uri.EscapeDataString(request.SpreadsheetId);
        var range = Uri.EscapeDataString(request.SheetName);
        var url = $"{BaseUrl}/{spreadsheetId}/values/{range}:append" +
                  "?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";
        using var httpRequest = await CreateRequestAsync(
            HttpMethod.Post,
            url,
            JsonContent.Create(new { values = new[] { request.Values } }, options: JsonOptions), cancellationToken);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<IReadOnlyList<object>>> ReadRowsAsync(
        string spreadsheetId,
        string range,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(range);

        var url = $"{BaseUrl}/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(range)}";
        using var request = await CreateRequestAsync(HttpMethod.Get, url, null, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<GoogleValueRange>(JsonOptions, cancellationToken);
        return result?.Values?
            .Select(row => (IReadOnlyList<object>)row.Select(ConvertValue).ToList())
            .ToList() ?? [];
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, HttpContent? content, CancellationToken cancellationToken)
    {
        var accessToken = await _tokens.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "Google Workspace no está conectado. Conecta la cuenta en Integraciones o configura Google:AccessToken antes de usar Sheets.");
        }

        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static object ConvertValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => string.Empty,
        _ => value.GetRawText()
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Google Sheets API respondió {(int)response.StatusCode} ({response.ReasonPhrase}): {body}",
            null,
            response.StatusCode);
    }

    private sealed record GoogleValueRange(List<List<JsonElement>>? Values);
}
