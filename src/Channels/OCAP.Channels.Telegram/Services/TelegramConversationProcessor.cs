using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Telegram.Services;

/// <summary>
/// Responde con el asistente un update de un bot registrado. Quien llama ya fijó la organización del scope
/// (<c>ITenantScope</c>) y conoce el token del bot. Solo chats privados con texto; se ignoran grupos, canales,
/// mensajes editados y los que envían otros bots.
/// </summary>
public sealed class TelegramConversationProcessor
{
    private readonly TelegramApiClient _apiClient;
    private readonly IConversationResponder _responder;
    private readonly ILogger<TelegramConversationProcessor> _logger;

    public TelegramConversationProcessor(
        TelegramApiClient apiClient,
        IConversationResponder responder,
        ILogger<TelegramConversationProcessor> logger)
    {
        _apiClient = apiClient;
        _responder = responder;
        _logger = logger;
    }

    /// <returns>true si se respondió; false si el update no se procesa o el envío falló.</returns>
    public async Task<bool> ProcessAsync(Guid tenantId, string botToken, TelegramUpdate update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message is null
            || string.IsNullOrWhiteSpace(message.Text)
            || !string.Equals(message.Chat.Type, "private", StringComparison.Ordinal)
            || message.From?.IsBot == true)
        {
            return false;
        }

        var incoming = TelegramWebhookMapper.ToIncomingMessage(update);
        var displayName = string.Join(' ', new[] { message.From?.FirstName, message.From?.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        if (string.IsNullOrWhiteSpace(displayName)) displayName = message.From?.Username ?? incoming.ExternalUserId;

        var reply = await _responder.RespondAsync(tenantId, incoming, displayName, cancellationToken);

        // Texto plano: las respuestas del modelo suelen traer Markdown que Telegram rechaza si no está balanceado.
        var sent = await _apiClient.SendMessageAsync(
            new TelegramSendMessageRequest { ChatId = incoming.ExternalUserId, Text = reply, ParseMode = null },
            botToken,
            cancellationToken);
        if (!sent)
        {
            _logger.LogWarning("No se pudo enviar la respuesta de Telegram de la organización {TenantId}.", tenantId);
        }
        return sent;
    }
}
