namespace OCAP.Core.Entities;

/// <summary>
/// Ajustes de toda la instalación que se editan en el panel (sección Sistema). Fila única, sin organización: un valor
/// null usa el de la configuración del servidor.
/// </summary>
public class SystemSettings
{
    public const int SingletonId = 1;

    public int Id { get; private set; } = SingletonId;
    public bool? RateLimitingEnabled { get; set; }
    public int? RequestsPerMinute { get; set; }
    public int? AuthAttemptsPerMinute { get; set; }
    public int? WidgetMessagesPerMinute { get; set; }
    public int? WidgetReadsPerMinute { get; set; }
    public int? SessionMinutes { get; set; }
    /// <summary>Orígenes que pueden llamar a la API desde un navegador, además de los del servidor.</summary>
    public List<string> AllowedOrigins { get; set; } = [];
    public DateTime UpdatedAtUtc { get; set; }
}
