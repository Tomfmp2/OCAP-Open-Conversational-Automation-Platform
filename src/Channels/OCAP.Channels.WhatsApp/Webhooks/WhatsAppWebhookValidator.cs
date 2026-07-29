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

        _logger.LogWarning("Validación de Webhook fallida. Mode: {Mode}, Token: {Token}", mode, verifyToken);
        return false;
    }

    public bool ValidateSignature(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(_settings.AppSecret))
        {
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

    public bool ValidatePayload(WhatsAppCloudWebhookPayload payload)
    {
        if (payload == null || payload.Object != "whatsapp_business_account" || payload.Entry == null || !payload.Entry.Any())
        {
            return false;
        }

        var change = payload.Entry.FirstOrDefault()?.Changes?.FirstOrDefault();
        if (change == null || change.Field != "messages")
        {
            return false;
        }

        return true;
    }
}
