using System.Text.RegularExpressions;

namespace OCAP.Api.Services;

/// <summary>
/// Traduce los fallos de Calendar, Gmail y Sheets a algo que la persona pueda resolver. Sin esto el asistente decía
/// «prueba el modelo de IA» cuando el modelo estaba bien y el problema era de la cuenta de Google.
/// </summary>
public static partial class GoogleErrorMessages
{
    [GeneratedRegex(@"^(Google Calendar|Gmail|Google Sheets) API respondió (\d{3})")]
    private static partial Regex GoogleFailure();

    /// <summary>Mensaje para la persona, o null si el error no viene de un servicio de Google.</summary>
    public static string? Describe(Exception exception)
    {
        var match = GoogleFailure().Match(exception.Message);
        if (!match.Success) return null;

        var service = match.Groups[1].Value;
        var status = match.Groups[2].Value;
        var detail = exception.Message;

        if (detail.Contains("has not been used in project", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("SERVICE_DISABLED", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("accessNotConfigured", StringComparison.OrdinalIgnoreCase))
            return $"{service} no está activado en tu proyecto de Google Cloud. Actívalo en la consola de Google "
                + "(APIs y servicios → Biblioteca), espera unos minutos y vuelve a pedírmelo.";

        if (status == "401" || detail.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
            return $"Google no autorizó el acceso a {service}. Vuelve a conectar Google en Organización y acepta todos los permisos.";

        if (status == "429" || detail.Contains("rateLimitExceeded", StringComparison.OrdinalIgnoreCase))
            return $"{service} recibió demasiadas peticiones. Espera un momento y vuelve a intentarlo.";

        return $"{service} rechazó la operación (código {status}). Revisa la conexión con Google en Organización.";
    }
}
