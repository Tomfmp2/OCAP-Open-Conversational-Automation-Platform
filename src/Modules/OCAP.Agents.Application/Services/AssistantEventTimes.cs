using System.Globalization;

namespace OCAP.Agents.Application.Services;

/// <summary>
/// Pasa las horas de un evento que propone el modelo a UTC. El modelo escribe la hora tal como la dijo la persona
/// («mañana a las 9» → 09:00) y aquí se interpreta en la zona de la organización: dejar esa cuenta al modelo creaba
/// citas a otra hora.
/// </summary>
public static class AssistantEventTimes
{
    private static readonly string[] Keys = ["StartDate", "EndDate"];

    /// <summary>Reescribe StartDate y EndDate en UTC. Devuelve la hora local de inicio, o null si falta o no se entiende.</summary>
    public static DateTime? NormalizeToUtc(Dictionary<string, object> args, TimeZoneInfo zone)
    {
        DateTime? localStart = null;
        foreach (var key in Keys)
        {
            if (!args.TryGetValue(key, out var raw) || raw?.ToString() is not { Length: > 0 } text) continue;
            if (ToUtc(text, zone) is not { } utc)
            {
                if (key == "StartDate") return null;
                args.Remove(key);
                continue;
            }

            args[key] = utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            if (key == "StartDate") localStart = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        }
        return localStart;
    }

    private static DateTime? ToUtc(string text, TimeZoneInfo zone)
    {
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) return null;
        return parsed.Kind switch
        {
            DateTimeKind.Utc => parsed,
            DateTimeKind.Local => parsed.ToUniversalTime(),
            // Sin zona en el texto: es la hora de reloj de la organización.
            _ => TimeZoneInfo.ConvertTimeToUtc(parsed, zone)
        };
    }
}
