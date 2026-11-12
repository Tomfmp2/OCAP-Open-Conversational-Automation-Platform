using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;
using OCAP.Tools.Abstractions;

namespace OCAP.Tools.Google;

// Herramienta ejecutable por agentes para la creación de eventos en Google Calendar.
public class CreateCalendarEventTool : ITool
{
    private readonly ICalendarProvider _calendarProvider;

    public ToolDefinition Definition { get; } = new()
    {
        Id = "google.calendar.create_event",
        Name = "CreateCalendarEventTool",
        Description = "Crea un evento en el calendario de Google Workspace.",
        Version = "1.0.0",
        RequiredPermissions = new List<string> { "Calendar.Create" },
        InputSchema = "{ \"Title\": \"string\", \"Description\": \"string\", \"StartDate\": \"datetime\", \"EndDate\": \"datetime\", \"Attendees\": [\"string\"] }",
        OutputSchema = "{ \"EventId\": \"string\", \"Title\": \"string\", \"Status\": \"created\" }"
    };

    public CreateCalendarEventTool(ICalendarProvider calendarProvider)
    {
        _calendarProvider = calendarProvider ?? throw new ArgumentNullException(nameof(calendarProvider));
    }

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (!context.Parameters.TryGetValue("Title", out var titleObj) || titleObj is not string title || string.IsNullOrWhiteSpace(title))
        {
            return ToolResult.Fail("INVALID_PARAMETER", "El parámetro 'Title' es obligatorio.");
        }

        var description = context.Parameters.TryGetValue("Description", out var descObj) ? descObj?.ToString() ?? string.Empty : string.Empty;
        
        // Sin fecha válida no se crea nada: antes se agendaba «ahora» en silencio.
        if (!context.Parameters.TryGetValue("StartDate", out var startObj) || !TryParseUtc(startObj, out var startDate))
        {
            return ToolResult.Fail("INVALID_PARAMETER", "Falta la fecha y hora del evento o no se entiende.");
        }

        var endDate = startDate.AddHours(1);
        if (context.Parameters.TryGetValue("EndDate", out var endObj) && TryParseUtc(endObj, out var parsedEnd) && parsedEnd > startDate)
        {
            endDate = parsedEnd;
        }

        var attendees = new List<string>();
        if (context.Parameters.TryGetValue("Attendees", out var attObj) && attObj is IEnumerable<string> list)
        {
            attendees.AddRange(list);
        }

        var calendarEvent = new CalendarEvent
        {
            Title = title,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            Attendees = attendees
        };

        var created = await _calendarProvider.CreateEventAsync(calendarEvent, cancellationToken);
        return ToolResult.Ok(created, "Evento de calendario creado con éxito.");
    }

    private static bool TryParseUtc(object? value, out DateTime utc) =>
        DateTime.TryParse(
            value?.ToString(),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out utc);
}
