using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Calendar;

// Implementación en memoria del proveedor de Google Calendar para entornos aislados y pruebas.
public class InMemoryCalendarProvider : ICalendarProvider
{
    private readonly List<CalendarEvent> _events = new();

    public Task<CalendarEvent> CreateEventAsync(CalendarEvent eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null) throw new ArgumentNullException(nameof(eventData));
        
        _events.Add(eventData);
        return Task.FromResult(eventData);
    }

    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var result = _events
            .Where(e => e.StartDate >= startDate && e.EndDate <= endDate)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<CalendarEvent>>(result);
    }

    public Task<bool> DeleteEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var removed = _events.RemoveAll(e => e.Id == eventId) > 0;
        return Task.FromResult(removed);
    }
}
