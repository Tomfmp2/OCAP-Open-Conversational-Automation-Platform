namespace OCAP.Channels.WhatsApp.Configuration;

public class WhatsAppSettings
{
    public const string SectionName = "WhatsAppCloud";

    // Habilitar o deshabilitar el proveedor de WhatsApp
    public bool Enabled { get; set; } = true;

    // Token de acceso de la aplicación (System User Token)
    public string ApiToken { get; set; } = string.Empty;
    
    // Secreto de la aplicación (App Secret) usado para validar webhooks
    public string AppSecret { get; set; } = string.Empty;

    // Token arbitrario usado para verificar la configuración del Webhook
    public string WebhookVerifyToken { get; set; } = string.Empty;
}
