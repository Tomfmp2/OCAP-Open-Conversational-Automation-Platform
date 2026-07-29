using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OCAP.Application.UseCases;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Core.Events;

namespace OCAP.Channels.Telegram.Services;

// Receptor de mensajes del canal Telegram que implementa IMessageReceiver.
// Recibe mensajes entrantes mapeados y los canaliza hacia el pipeline conversacional en Application Layer.
public class TelegramMessageReceiver : IMessageReceiver
{
    private readonly ReceiveMessageUseCase _receiveMessageUseCase;
    private readonly ILogger<TelegramMessageReceiver> _logger;
    private readonly IEventBus? _eventBus;

    public TelegramMessageReceiver(
        ReceiveMessageUseCase receiveMessageUseCase,
        ILogger<TelegramMessageReceiver> logger,
        IEventBus? eventBus = null)
    {
        _receiveMessageUseCase = receiveMessageUseCase;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<bool> ReceiveMessageAsync(IncomingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.ExternalUserId))
        {
            _logger.LogWarning("Se recibió un mensaje nulo o con ExternalUserId vacío en TelegramMessageReceiver.");
            return false;
        }

        try
        {
            var userId = ParseOrGenerateGuid(message.ExternalUserId);

            _logger.LogInformation("Entregando mensaje Telegram de usuario externo {ExternalUser} (Guid: {UserId}) a ReceiveMessageUseCase.",
                message.ExternalUserId, userId);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new MessageReceivedEvent("Telegram", message.ExternalUserId, message.Message ?? string.Empty, Guid.Empty), cancellationToken);
            }

            await _receiveMessageUseCase.ExecuteAsync(userId, message.Message, "Telegram", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar el mensaje entrante de Telegram para el usuario {User}.", message.ExternalUserId);
            return false;
        }
    }

    // Mapea un identificador de chat externo a un Guid determinista para interactuar con la capa Application.
    private static Guid ParseOrGenerateGuid(string externalUserId)
    {
        if (Guid.TryParse(externalUserId, out var guid))
        {
            return guid;
        }

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"OCAP_Telegram_{externalUserId}"));
        return new Guid(hash);
    }
}
