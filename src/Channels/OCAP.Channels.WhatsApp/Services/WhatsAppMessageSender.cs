using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Core.Events;

namespace OCAP.Channels.WhatsApp.Services;

public class WhatsAppMessageSender : IMessageSender
{
    private readonly EvolutionApiClient _evolutionClient;
    private readonly WhatsAppApiClient? _cloudClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppMessageSender> _logger;
    private readonly IEventBus? _eventBus;

    public WhatsAppMessageSender(
        EvolutionApiClient evolutionClient,
        ILogger<WhatsAppMessageSender> logger,
        WhatsAppApiClient? cloudClient = null,
        IOptions<WhatsAppSettings>? settings = null,
        IEventBus? eventBus = null)
    {
        _evolutionClient = evolutionClient;
        _cloudClient = cloudClient;
        _settings = settings?.Value ?? new WhatsAppSettings { Provider = "Evolution" };
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<bool> SendMessageAsync(OutgoingChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.DestinationUserId) || string.IsNullOrWhiteSpace(message.Message))
        {
            _logger.LogWarning("Se intentó enviar un OutgoingChannelMessage nulo o inválido en WhatsAppMessageSender.");
            return false;
        }

        try
        {
            message.Metadata.TryGetValue("ConnectionMode", out var mode);
            var useEvolution =
                string.Equals(mode, "evolution", StringComparison.OrdinalIgnoreCase) ||
                (_settings.IsEvolution && !string.Equals(mode, "cloud", StringComparison.OrdinalIgnoreCase));

            bool success;
            if (useEvolution || _cloudClient == null)
            {
                var instance = message.Metadata.TryGetValue("Instance", out var inst) && !string.IsNullOrWhiteSpace(inst)
                    ? inst!
                    : (string.IsNullOrWhiteSpace(_settings.Instance) ? "ocap-main" : _settings.Instance);
                success = await _evolutionClient.SendTextAsync(instance, message.DestinationUserId, message.Message, cancellationToken);
            }
            else
            {
                var request = new WhatsAppCloudSendMessageRequest
                {
                    To = message.DestinationUserId,
                    Text = new WhatsAppCloudText { Body = message.Message }
                };
                message.Metadata.TryGetValue("PhoneNumberId", out string? phoneNumberId);
                message.Metadata.TryGetValue("ApiToken", out string? overrideToken);

                if (string.IsNullOrWhiteSpace(phoneNumberId))
                {
                    _logger.LogError("PhoneNumberId ausente para envío Cloud API.");
                    return false;
                }

                success = await _cloudClient.SendMessageAsync(phoneNumberId, request, overrideToken, cancellationToken);
            }

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(
                    new MessageSentEvent("WhatsApp", message.DestinationUserId, message.Message, success, Guid.Empty),
                    cancellationToken);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al despachar mensaje WhatsApp a {Destination}.", message.DestinationUserId);
            return false;
        }
    }
}
