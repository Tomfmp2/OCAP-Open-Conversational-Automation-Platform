using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Calendar;

public sealed class GoogleCalendarHttpProvider : ICalendarProvider
{
    private const string BaseUrl = "https://www.googleapis.com/calendar/v3";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly GoogleWorkspaceOptions _options;

    public GoogleCalendarHttpProvider(HttpClient httpClient, IOptions<GoogleWorkspaceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<CalendarEvent> CreateEventAsync(
        CalendarEvent eventData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        using var request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/calendars/primary/events",
            JsonContent.Create(ToGoogleEvent(eventData), options: JsonOptions));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<GoogleEvent>(JsonOptions, cancellationToken);
        return result is null
            ? throw new InvalidOperationException("Google Calendar devolvió una respuesta vacía.")
            : ToCalendarEvent(result);
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial.", nameof(endDate));
        }

        var query = $"timeMin={Uri.EscapeDataString(ToRfc3339(startDate))}" +
                    $"&timeMax={Uri.EscapeDataString(ToRfc3339(endDate))}&singleEvents=true&orderBy=startTime";
        using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/calendars/primary/events?{query}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<GoogleEventList>(JsonOptions, cancellationToken);
        return result?.Items?.Select(ToCalendarEvent).ToList() ?? [];
    }

    public async Task<bool> DeleteEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        using var request = CreateRequest(
            HttpMethod.Delete,
            $"{BaseUrl}/calendars/primary/events/{Uri.EscapeDataString(eventId)}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException(
                "Google Workspace no está configurado. Configure Google:AccessToken antes de usar Calendar.");
        }

        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return request;
    }

    private static object ToGoogleEvent(CalendarEvent value) => new
    {
        summary = value.Title,
        description = value.Description,
        start = new { dateTime = ToRfc3339(value.StartDate) },
        end = new { dateTime = ToRfc3339(value.EndDate) },
        attendees = value.Attendees.Select(email => new { email }).ToArray()
    };

    private static CalendarEvent ToCalendarEvent(GoogleEvent value) => new()
    {
        Id = value.Id ?? string.Empty,
        Title = value.Summary ?? string.Empty,
        Description = value.Description ?? string.Empty,
        StartDate = ParseGoogleDate(value.Start),
        EndDate = ParseGoogleDate(value.End),
        Attendees = value.Attendees?.Select(a => a.Email).Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? []
    };

    private static DateTime ParseGoogleDate(GoogleEventDate? value)
    {
        var text = value?.DateTime ?? value?.Date;
        return DateTimeOffset.TryParse(text, out var parsed) ? parsed.UtcDateTime : default;
    }

    private static string ToRfc3339(DateTime value) =>
        new DateTimeOffset(value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value).ToUniversalTime().ToString("O");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Google Calendar API respondió {(int)response.StatusCode} ({response.ReasonPhrase}): {body}",
            null,
            response.StatusCode);
    }

    private sealed record GoogleEventList(List<GoogleEvent>? Items);
    private sealed record GoogleEvent(
        string? Id,
        string? Summary,
        string? Description,
        GoogleEventDate? Start,
        GoogleEventDate? End,
        List<GoogleAttendee>? Attendees);
    private sealed record GoogleEventDate(string? DateTime, string? Date);
    private sealed record GoogleAttendee(string Email);
}
