namespace OCAP.Providers.Google.Abstractions.Models;

// DTO de evento de calendario de Google.
public class CalendarEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> Attendees { get; set; } = new();
}
