using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Sheets;

public sealed class GoogleSheetsHttpProvider : ISpreadsheetProvider
{
    private const string BaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<GoogleWorkspaceOptions> _options;

    public GoogleSheetsHttpProvider(HttpClient httpClient, IOptionsMonitor<GoogleWorkspaceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
        using var httpRequest = CreateRequest(
            HttpMethod.Post,
            url,
            JsonContent.Create(new { values = new[] { request.Values } }, options: JsonOptions));
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
        using var request = CreateRequest(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<GoogleValueRange>(JsonOptions, cancellationToken);
        return result?.Values?
            .Select(row => (IReadOnlyList<object>)row.Select(ConvertValue).ToList())
            .ToList() ?? [];
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var accessToken = _options.CurrentValue.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "Google Workspace no está configurado. Configure Google:AccessToken antes de usar Sheets.");
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
