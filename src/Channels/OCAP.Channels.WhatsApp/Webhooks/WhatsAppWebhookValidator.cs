using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Webhooks;

// Validador de seguridad para payloads y peticiones de webhook entrantes.
public class WhatsAppWebhookValidator
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppWebhookValidator> _logger;

    // Límite máximo para el contenido del texto de un mensaje en bytes (10 KB).
    private const int MaxMessageBytes = 10 * 1024;

    public WhatsAppWebhookValidator(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppWebhookValidator> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    // Valida que el payload del webhook contenga la estructura y los datos mínimos necesarios.
    public bool ValidatePayload(WhatsAppWebhookPayload? payload)
    {
        if (payload == null)
        {
            _logger.LogWarning("Webhook WhatsApp rechazado: payload es nulo.");
            return false;
        }

        if (payload.Data?.Key == null || string.IsNullOrWhiteSpace(payload.Data.Key.RemoteJid))
        {
            _logger.LogWarning("Webhook WhatsApp rechazado: falta remoteJid o clave de mensaje.");
            return false;
        }

        // Ignorar mensajes enviados por la propia instancia (fromMe = true).
        if (payload.Data.Key.FromMe)
        {
            _logger.LogDebug("Webhook ignorado: el mensaje fue enviado por la propia instancia.");
            return false;
        }

        var text = GetMessageText(payload);
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Webhook WhatsApp rechazado: el mensaje de texto está vacío o no es soportado.");
            return false;
        }

        if (text.Length > MaxMessageBytes)
        {
            _logger.LogWarning("Webhook WhatsApp rechazado: supera el tamaño máximo permitido ({MaxBytes} bytes).", MaxMessageBytes);
            return false;
        }

        return true;
    }

    // Valida el token o secreto de webhook opcional en los headers de la petición HTTP.
    public bool ValidateSecret(string? providedSecret)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            // Si no hay secreto configurado, se permite la entrada (fail-open controlado en dev).
            return true;
        }

        var isValid = string.Equals(_settings.WebhookSecret, providedSecret, StringComparison.Ordinal);
        if (!isValid)
        {
            _logger.LogWarning("Firma/Secreto de webhook de WhatsApp no coincide con el token configurado.");
        }

        return isValid;
    }

    // Extrae el texto del mensaje desde diferentes tipos de payload de Evolution API.
    public static string GetMessageText(WhatsAppWebhookPayload payload)
    {
        var conversation = payload.Data?.Message?.Conversation;
        if (!string.IsNullOrWhiteSpace(conversation))
        {
            return conversation;
        }

        var extended = payload.Data?.Message?.ExtendedTextMessage?.Text;
        if (!string.IsNullOrWhiteSpace(extended))
        {
            return extended;
        }

        return string.Empty;
    }
}
