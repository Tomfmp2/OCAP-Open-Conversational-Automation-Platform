using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Telegram.Services;

// Emisor del canal Telegram que implementa IMessageSender para despachar mensajes a usuarios externos.
public class TelegramMessageSender : IMessageSender
{
    private readonly TelegramApiClient _apiClient;
    private readonly ILogger<TelegramMessageSender> _logger;

    public TelegramMessageSender(
        TelegramApiClient apiClient,
        ILogger<TelegramMessageSender> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.DestinationUserId) || string.IsNullOrWhiteSpace(message.Message))
        {
            _logger.LogWarning("Se intentó enviar un OutgoingChannelMessage nulo o inválido en TelegramMessageSender.");
            return false;
        }

        try
        {
            var request = TelegramWebhookMapper.ToSendMessageRequest(message);
            var success = await _apiClient.SendMessageAsync(request, cancellationToken: cancellationToken);

            if (success)
            {
                _logger.LogInformation("Mensaje de respuesta despachado exitosamente a Telegram para ChatId {ChatId}.", message.DestinationUserId);
            }
            else
            {
                _logger.LogWarning("Falló el despacho del mensaje de respuesta a Telegram para ChatId {ChatId}.", message.DestinationUserId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado al despachar mensaje Telegram a {Destination}.", message.DestinationUserId);
            return false;
        }
    }
}
