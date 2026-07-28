using Microsoft.Extensions.Options;
using OCAP.Channels.Telegram.Configuration;
using OCAP.Channels.Telegram.DTOs;

namespace OCAP.Channels.Telegram.Webhooks;

// Validador de seguridad y estructura defensiva para peticiones de webhook recibidas desde Telegram.
public class TelegramWebhookValidator
{
    private readonly TelegramOptions _options;

    public TelegramWebhookValidator(IOptions<TelegramOptions> options)
    {
        _options = options.Value;
    }

    // Valida el secreto de seguridad recibido en el encabezado X-Telegram-Bot-Api-Secret-Token.
    public bool ValidateSecret(string? secretHeader)
    {
        if (!_options.EnableWebhookValidation)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_options.SecretToken))
        {
            return true;
        }

        return string.Equals(_options.SecretToken, secretHeader, StringComparison.Ordinal);
    }

    // Valida la integridad del contenido del TelegramUpdate recibido.
    public bool ValidatePayload(TelegramUpdate? update)
    {
        if (update == null)
        {
            return false;
        }

        var message = update.Message ?? update.EditedMessage;
        if (message == null || message.Chat == null || message.Chat.Id == 0)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(message.Text);
    }
}
