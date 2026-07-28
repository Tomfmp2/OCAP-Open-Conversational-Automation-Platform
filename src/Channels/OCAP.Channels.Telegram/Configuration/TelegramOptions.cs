namespace OCAP.Channels.Telegram.Configuration;

// Opciones de configuración para el adaptador nativo del canal Telegram.
public class TelegramOptions
{
    public const string SectionName = "Telegram";

    // Token secreto otorgado por Telegram BotFather.
    public string BotToken { get; set; } = string.Empty;

    // Token secreto de encabezado (X-Telegram-Bot-Api-Secret-Token) para validar el origen del webhook.
    public string SecretToken { get; set; } = string.Empty;

    // URL pública para la recepción de eventos por webhook.
    public string WebhookUrl { get; set; } = string.Empty;

    // Flag para habilitar/deshabilitar la validación estricta del secreto del webhook.
    public bool EnableWebhookValidation { get; set; } = true;
}
