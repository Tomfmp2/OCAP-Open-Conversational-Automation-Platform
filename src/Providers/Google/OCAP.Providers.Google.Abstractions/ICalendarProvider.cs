using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Abstractions;

// Contrato desacoplado para servicios de Google Calendar sin depender de SDKs de terceros.
public interface ICalendarProvider
{
    // Crea un evento en el calendario del usuario.
    Task<CalendarEvent> CreateEventAsync(CalendarEvent eventData, CancellationToken cancellationToken = default);

    // Consulta los eventos dentro de una ventana de tiempo.
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Elimina un evento del calendario por su ID.
    Task<bool> DeleteEventAsync(string eventId, CancellationToken cancellationToken = default);
}
