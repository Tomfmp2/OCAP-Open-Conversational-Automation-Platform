namespace OCAP.Api.Configuration;

// Configuración de Rate Limiting cargada desde appsettings.
// Permite ajustar los límites por ambiente sin recompilar.
public class RateLimitingSettings
{
    // Clave usada en appsettings.json para enlazar esta sección.
    public const string SectionName = "RateLimiting";

    // Habilita o deshabilita el rate limiting (útil para deshabilitar en testing).
    public bool EnableRateLimiting { get; set; } = true;

    // Número máximo de peticiones permitidas dentro de la ventana de tiempo.
    public int PermitLimit { get; set; } = 100;

    // Tamaño de la ventana deslizante en segundos.
    public int WindowSeconds { get; set; } = 60;

    // Número de peticiones en cola que se procesan cuando se excede el límite.
    public int QueueLimit { get; set; } = 10;
}
