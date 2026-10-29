namespace OCAP.Infrastructure.Services;

/// <summary>
/// Preferencias de la organización editables en el panel (Tenants.SettingsJson, "Preferences"). Lo que no se indica
/// sigue la configuración del servidor.
/// </summary>
/// <param name="Language">"es" o "en": idioma de las respuestas del asistente.</param>
/// <param name="TimeZone">Zona horaria IANA (America/Bogota): fechas que el asistente entiende y crea.</param>
/// <param name="PanelUrl">Dirección pública del panel: enlaces de invitación y retorno de Google.</param>
/// <param name="ConversationRetentionDays">Días que se guardan las conversaciones con clientes; null, para siempre.</param>
public sealed record OrganizationPreferences(string? Language, string? TimeZone, string? PanelUrl, int? ConversationRetentionDays)
{
    public const string Section = "Preferences";
    public const int MinRetentionDays = 7;
    public const int MaxRetentionDays = 3650;
}
