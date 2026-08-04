using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.DTOs;

namespace OCAP.Channels.WhatsApp.Webhooks;

public static class WhatsAppWebhookMapper
{
    public static IncomingChannelMessage? ToIncomingMessage(WhatsAppCloudWebhookPayload payload)
    {
        var change = payload.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault();
        if (change?.Value == null) return null;
        if (change.Value.Messages == null || !change.Value.Messages.Any()) return null;

        var message = change.Value.Messages.First();
        var contact = change.Value.Contacts?.FirstOrDefault();
        var metadata = change.Value.Metadata;

        var incoming = new IncomingChannelMessage
        {
            ExternalUserId = message.From,
            ChannelName = "WhatsApp",
            Message = message.Text?.Body ?? string.Empty
        };

        if (long.TryParse(message.Timestamp, out var timestamp))
        {
            incoming.ReceivedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        incoming.Metadata["MessageId"] = message.Id;
        incoming.Metadata["MessageType"] = message.Type;
        incoming.Metadata["ConnectionMode"] = "cloud";

        if (contact != null)
        {
            incoming.Metadata["SenderName"] = contact.Profile?.Name ?? string.Empty;
            incoming.Metadata["WaId"] = contact.WaId;
        }

        if (metadata != null)
        {
            incoming.Metadata["PhoneNumberId"] = metadata.PhoneNumberId;
            incoming.Metadata["DisplayPhoneNumber"] = metadata.DisplayPhoneNumber;
        }

        return incoming;
    }

    public static IncomingChannelMessage? ToIncomingMessage(WhatsAppWebhookPayload payload)
    {
        if (payload?.Data?.Key == null || payload.Data.Key.FromMe) return null;

        var text = payload.Data.Message?.Conversation
                   ?? payload.Data.Message?.ExtendedTextMessage?.Text
                   ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return null;

        var remoteJid = payload.Data.Key.RemoteJid ?? string.Empty;
        var externalId = remoteJid.Contains('@') ? remoteJid.Split('@')[0] : remoteJid;

        var incoming = new IncomingChannelMessage
        {
            ExternalUserId = externalId,
            ChannelName = "WhatsApp",
            Message = text,
            ReceivedAt = payload.Data.MessageTimestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(payload.Data.MessageTimestamp).UtcDateTime
                : DateTime.UtcNow
        };

        incoming.Metadata["MessageId"] = payload.Data.Key.Id;
        incoming.Metadata["SenderName"] = payload.Data.PushName;
        incoming.Metadata["Instance"] = payload.Instance;
        incoming.Metadata["ConnectionMode"] = "evolution";
        incoming.Metadata["RemoteJid"] = remoteJid;
        return incoming;
    }
}
