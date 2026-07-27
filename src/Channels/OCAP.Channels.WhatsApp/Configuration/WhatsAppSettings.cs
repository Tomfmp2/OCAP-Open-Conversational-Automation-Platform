namespace OCAP.Channels.WhatsApp.Configuration;

// Configuración fuertemente tipada de integración con Evolution API para WhatsApp.
public class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    // Habilita o deshabilita la integración del canal de WhatsApp.
    public bool Enabled { get; set; } = false;

    // URL base del servidor de Evolution API (ej. "http://localhost:8080").
    public string BaseUrl { get; set; } = string.Empty;

    // Nombre de la instancia activa configurada en Evolution API (ej. "ocap-main").
    public string Instance { get; set; } = string.Empty;

    // API Key de autenticación asignada en Evolution API.
    public string ApiKey { get; set; } = string.Empty;

    // Token secreto opcional para validar la firma de seguridad de los webhooks recibidos.
    public string WebhookSecret { get; set; } = string.Empty;
}
