using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.DTOs;

namespace OCAP.Channels.WhatsApp.Webhooks;

public class WhatsAppWebhookValidator
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppWebhookValidator> _logger;

    public WhatsAppWebhookValidator(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppWebhookValidator> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool ValidateVerifyToken(string mode, string verifyToken)
    {
        if (mode == "subscribe" && verifyToken == _settings.WebhookVerifyToken)
        {
            return true;
        }

        _logger.LogWarning("Validación de Webhook fallida. Mode: {Mode}", mode);
        return false;
    }

    /// <summary>Firma con el App Secret del servidor (WhatsAppCloud:AppSecret), si existe.</summary>
    public bool ValidateSignature(string payload, string? signatureHeader) =>
        !string.IsNullOrWhiteSpace(_settings.AppSecret) && SignatureMatches(payload, signatureHeader, _settings.AppSecret);

    /// <summary>X-Hub-Signature-256 de Meta: HMAC-SHA256 del cuerpo con el App Secret, comparado en tiempo constante.</summary>
    public static bool SignatureMatches(string payload, string? signatureHeader, string appSecret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(appSecret)) return false;
        var expected = signatureHeader.StartsWith("sha256=", StringComparison.Ordinal) ? signatureHeader[7..] : signatureHeader;
        var actual = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(payload)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected.ToLowerInvariant()));
    }

    public bool ValidatePayload(WhatsAppCloudWebhookPayload payload)
    {
        if (payload == null || payload.Object != "whatsapp_business_account" || payload.Entry == null || !payload.Entry.Any())
        {
            return false;
        }

        var change = payload.Entry.FirstOrDefault()?.Changes?.FirstOrDefault();
        return change != null && change.Field == "messages";
    }
}
