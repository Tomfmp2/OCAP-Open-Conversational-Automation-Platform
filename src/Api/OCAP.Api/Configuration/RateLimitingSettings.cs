namespace OCAP.Api.Configuration;

// Configuración de Rate Limiting cargada desde appsettings.
// Permite ajustar los límites por ambiente sin recompilar.
public class RateLimitingSettings
{
    // Clave usada en appsettings.json para enlazar esta sección.
    public const string SectionName = "RateLimiting";

    // Política que limita login, refresh, configuración inicial y MFA (fuerza bruta).
    public const string AuthPolicy = "auth";

    // Habilita o deshabilita el límite global por IP (útil para deshabilitar en testing).
    public bool EnableRateLimiting { get; set; } = true;

    // Número máximo de peticiones permitidas por IP dentro de la ventana de tiempo.
    public int PermitLimit { get; set; } = 300;

    // Intentos de autenticación por IP y minuto; 0 desactiva la política (solo para pruebas).
    public int AuthPermitLimit { get; set; } = 10;

    // Tamaño de la ventana deslizante en segundos.
    public int WindowSeconds { get; set; } = 60;

    // Número de peticiones en cola que se procesan cuando se excede el límite.
    public int QueueLimit { get; set; } = 10;
}
