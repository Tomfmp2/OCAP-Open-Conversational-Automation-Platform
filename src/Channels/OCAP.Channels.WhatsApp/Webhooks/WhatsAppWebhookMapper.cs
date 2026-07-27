using OCAP.Channels.Abstractions.Models;

namespace OCAP.Channels.WhatsApp.Webhooks;

// Mapeador para transformar payloads específicos de Evolution API a IncomingChannelMessage de OCAP.
public static class WhatsAppWebhookMapper
{
    // Transforma un payload validado de Evolution API al modelo interno agnóstico de OCAP.
    public static IncomingChannelMessage ToIncomingMessage(WhatsAppWebhookPayload payload)
    {
        var rawJid = payload.Data?.Key?.RemoteJid ?? string.Empty;
        var cleanUserId = ExtractPhoneNumber(rawJid);
        var messageText = WhatsAppWebhookValidator.GetMessageText(payload);

        var timestamp = payload.Data?.MessageTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(payload.Data.MessageTimestamp).UtcDateTime
            : DateTime.UtcNow;

        return new IncomingChannelMessage
        {
            ExternalUserId = cleanUserId,
            Message = messageText.Trim(),
            ChannelName = "WhatsApp",
            ReceivedAt = timestamp,
            Metadata = new Dictionary<string, string>
            {
                ["RemoteJid"] = rawJid,
                ["PushName"] = payload.Data?.PushName ?? string.Empty,
                ["MessageId"] = payload.Data?.Key?.Id ?? string.Empty,
                ["Instance"] = payload.Instance ?? string.Empty
            }
        };
    }

    // Limpia el RemoteJid de WhatsApp para obtener únicamente el número telefónico.
    private static string ExtractPhoneNumber(string remoteJid)
    {
        if (string.IsNullOrWhiteSpace(remoteJid)) return string.Empty;
        var part = remoteJid.Split('@')[0];
        return new string(part.Where(char.IsDigit).ToArray());
    }
}
