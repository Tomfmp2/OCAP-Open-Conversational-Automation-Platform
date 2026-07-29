using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.DTOs;

namespace OCAP.Channels.WhatsApp.Webhooks;

public static class WhatsAppWebhookMapper
{
    public static IncomingChannelMessage? ToIncomingMessage(WhatsAppCloudWebhookPayload payload)
    {
        var change = payload.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault();
        if (change == null || change.Value == null) return null;

        var value = change.Value;
        
        // Si no hay mensajes entrantes, podría ser un evento de estado (delivery status)
        if (value.Messages == null || !value.Messages.Any())
        {
            return null; // O manejar actualizaciones de estado aquí si la abstracción lo soporta
        }

        var message = value.Messages.First();
        var contact = value.Contacts?.FirstOrDefault();
        var metadata = value.Metadata;

        var incoming = new IncomingChannelMessage
        {
            ExternalUserId = message.From,
            ChannelName = "WhatsApp",
            Message = message.Text?.Body ?? string.Empty
        };

        if (long.TryParse(message.Timestamp, out long timestamp))
        {
            incoming.ReceivedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        incoming.Metadata["MessageId"] = message.Id;
        incoming.Metadata["MessageType"] = message.Type;
        
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
}
