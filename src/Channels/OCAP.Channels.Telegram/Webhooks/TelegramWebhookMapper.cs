using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.Telegram.DTOs;

namespace OCAP.Channels.Telegram.Webhooks;

// Mapeador puro que desacopla las estructuras nativas de Telegram Bot API de los DTOs agnósticos de OCAP.
public static class TelegramWebhookMapper
{
    // Transforma un TelegramUpdate recibido por el webhook a un IncomingChannelMessage inmutable.
    public static IncomingChannelMessage ToIncomingMessage(TelegramUpdate update)
    {
        var message = update.Message ?? update.EditedMessage;
        if (message == null)
        {
            throw new ArgumentException("El update de Telegram no contiene un mensaje procesable.", nameof(update));
        }

        var externalUserId = message.Chat.Id.ToString();
        var messageText = message.Text ?? string.Empty;

        var metadata = new Dictionary<string, string>
        {
            ["update_id"] = update.UpdateId.ToString(),
            ["message_id"] = message.MessageId.ToString(),
            ["chat_type"] = message.Chat.Type
        };

        if (message.From != null)
        {
            metadata["sender_id"] = message.From.Id.ToString();
            metadata["first_name"] = message.From.FirstName;
            if (!string.IsNullOrWhiteSpace(message.From.LastName)) metadata["last_name"] = message.From.LastName;
            if (!string.IsNullOrWhiteSpace(message.From.Username)) metadata["username"] = message.From.Username;
        }

        return new IncomingChannelMessage
        {
            ExternalUserId = externalUserId,
            Message = messageText,
            ChannelName = "Telegram",
            ReceivedAt = DateTime.UtcNow,
            Metadata = metadata
        };
    }

    // Transforma un OutgoingChannelMessage agnóstico a una solicitud TelegramSendMessageRequest.
    public static TelegramSendMessageRequest ToSendMessageRequest(OutgoingChannelMessage message)
    {
        return new TelegramSendMessageRequest
        {
            ChatId = message.DestinationUserId,
            Text = message.Message,
            ParseMode = message.Metadata.TryGetValue("parse_mode", out var parseMode) ? parseMode : "Markdown"
        };
    }
}
