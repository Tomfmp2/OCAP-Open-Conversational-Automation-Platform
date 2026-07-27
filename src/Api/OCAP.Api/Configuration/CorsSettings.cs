namespace OCAP.Api.Configuration;

// Configuración de CORS cargada desde appsettings.
// Permite ajustar orígenes permitidos por ambiente sin recompilar.
public class CorsSettings
{
    // Clave usada en appsettings.json para enlazar esta sección.
    public const string SectionName = "Cors";

    // Lista de orígenes permitidos explícitamente.
    // Nunca usar AllowAnyOrigin() en producción: expone la API a ataques CSRF.
    public string[] AllowedOrigins { get; set; } = [];

    // Indica si se permiten credenciales (cookies, Authorization headers) en peticiones CORS.
    // Requiere que AllowedOrigins tenga valores explícitos; no funciona con AllowAnyOrigin.
    public bool AllowCredentials { get; set; } = false;
}
