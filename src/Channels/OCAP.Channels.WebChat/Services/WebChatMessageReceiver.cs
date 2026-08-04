using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OCAP.Application.UseCases;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Core.Entities;
using OCAP.Core.Events;
using OCAP.Core.Ports;
using ChannelMessageReceiver = OCAP.Channels.Abstractions.Contracts.IMessageReceiver;

namespace OCAP.Channels.WebChat.Services;

public class WebChatMessageReceiver : ChannelMessageReceiver
{
    private readonly ReceiveMessageUseCase _receiveMessageUseCase;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<WebChatMessageReceiver> _logger;
    private readonly IEventBus? _eventBus;

    public WebChatMessageReceiver(
        ReceiveMessageUseCase receiveMessageUseCase,
        IUserRepository userRepository,
        ILogger<WebChatMessageReceiver> logger,
        IEventBus? eventBus = null)
    {
        _receiveMessageUseCase = receiveMessageUseCase;
        _userRepository = userRepository;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<bool> ReceiveMessageAsync(IncomingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.ExternalUserId))
        {
            _logger.LogWarning("Mensaje WebChat nulo o sin ExternalUserId.");
            return false;
        }

        try
        {
            var userId = ResolveUserId(message.ExternalUserId);
            var tenantId = Guid.Empty;
            if (message.Metadata.TryGetValue("TenantId", out var tenantRaw) && Guid.TryParse(tenantRaw, out var parsedTenant))
            {
                tenantId = parsedTenant;
            }

            var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (existing == null)
            {
                var displayName = message.Metadata.TryGetValue("DisplayName", out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : $"WebChat {message.ExternalUserId}";
                await _userRepository.SaveAsync(new User(userId, displayName, tenantId), cancellationToken);
            }

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(
                    new MessageReceivedEvent("WebChat", message.ExternalUserId, message.Message ?? string.Empty, tenantId),
                    cancellationToken);
            }

            await _receiveMessageUseCase.ExecuteAsync(userId, message.Message ?? string.Empty, "WebChat", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar mensaje WebChat de {User}.", message.ExternalUserId);
            return false;
        }
    }

    public static Guid ResolveUserId(string externalUserId)
    {
        if (Guid.TryParse(externalUserId, out var guid))
        {
            return guid;
        }

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"OCAP_WebChat_{externalUserId}"));
        return new Guid(hash);
    }
}
