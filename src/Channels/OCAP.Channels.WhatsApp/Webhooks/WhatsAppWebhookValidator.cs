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

    public bool ValidateSignature(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(_settings.AppSecret))
        {
            // En Evolution no hay firma Meta; se valida con apikey/webhook secret aparte.
            return false;
        }

        try
        {
            var expectedSignature = signatureHeader.Replace("sha256=", "");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.AppSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return hashString == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar la firma del webhook de WhatsApp.");
            return false;
        }
    }

    public bool ValidateEvolutionSecret(string? headerSecret, string? apikeyHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) && string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            // Desarrollo local sin secreto configurado: aceptar.
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_settings.WebhookSecret) &&
            string.Equals(headerSecret, _settings.WebhookSecret, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey) &&
            string.Equals(apikeyHeader, _settings.ApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        _logger.LogWarning("Secreto Evolution webhook inválido.");
        return false;
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

    public bool ValidateEvolutionPayload(WhatsAppWebhookPayload? payload)
    {
        if (payload == null) return false;
        var evt = payload.Event ?? string.Empty;
        return evt.Contains("messages", StringComparison.OrdinalIgnoreCase)
               || evt.Contains("MESSAGES", StringComparison.OrdinalIgnoreCase)
               || string.Equals(evt, "messages.upsert", StringComparison.OrdinalIgnoreCase);
    }
}
