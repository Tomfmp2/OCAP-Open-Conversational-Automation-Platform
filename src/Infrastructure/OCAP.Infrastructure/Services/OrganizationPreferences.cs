namespace OCAP.Infrastructure.Services;

/// <summary>
/// Preferencias de la organización editables en el panel (Tenants.SettingsJson, "Preferences"). Lo que no se indica
/// sigue la configuración del servidor.
/// </summary>
/// <param name="Language">"es" o "en": idioma de las respuestas del asistente.</param>
/// <param name="TimeZone">Zona horaria IANA (America/Bogota): fechas que el asistente entiende y crea.</param>
/// <param name="PanelUrl">Dirección pública del panel: enlaces de invitación y retorno de Google.</param>
/// <param name="ConversationRetentionDays">Días que se guardan las conversaciones con clientes; null, para siempre.</param>
/// <param name="MemoryDays">Días que los agentes recuerdan a una persona desde la última vez que confirmó un dato; null usa
/// <see cref="DefaultMemoryDays"/> y 0 desactiva la memoria.</param>
/// <param name="PrivacyNotice">Aviso de tratamiento de datos que reciben los contactos nuevos con la primera respuesta.</param>
public sealed record OrganizationPreferences(
    string? Language,
    string? TimeZone,
    string? PanelUrl,
    int? ConversationRetentionDays,
    int? MemoryDays = null,
    string? PrivacyNotice = null)
{
    public const string Section = "Preferences";
    public const int MinRetentionDays = 7;
    public const int MaxRetentionDays = 3650;
    public const int DefaultMemoryDays = 90;
    public const int MaxPrivacyNoticeLength = 600;

    /// <summary>Cuánto dura un recuerdo; null si la organización desactivó la memoria.</summary>
    public TimeSpan? MemoryLifetime => (MemoryDays ?? DefaultMemoryDays) is var days and > 0 ? TimeSpan.FromDays(days) : null;
}
