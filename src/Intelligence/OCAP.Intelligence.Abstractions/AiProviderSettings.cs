namespace OCAP.Intelligence.Abstractions;

// Configuración de credenciales y parámetros de ejecución para proveedores de IA.
public class AiProviderSettings
{
    // Clave de API obtenida mediante variables de entorno o gestores de secretos.
    public string ApiKey { get; set; } = string.Empty;

    // Identificador de organización opcional para servicios multi-tenant.
    public string OrganizationId { get; set; } = string.Empty;

    // URL base del endpoint del servicio de IA (ej. para Ollama o proxies enterprise).
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    // Nombre del modelo por defecto a utilizar.
    public string ModelName { get; set; } = "gpt-4o";

    // Máximo número de tokens permitidos en la respuesta.
    public int MaxTokens { get; set; } = 2048;

    // Parámetro de temperatura de muestreo (0.0 para respuestas deterministas, 1.0 para mayor creatividad).
    public double Temperature { get; set; } = 0.7;
}
