using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Channels.Telegram.Services;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Channels.WhatsApp.Services;
using OCAP.Core.Entities;

namespace OCAP.Api.Services;

/// <summary>
/// Envía un mensaje del equipo a un contacto por el mismo canal y conexión por los que escribió (registrados en la
/// conversación): el número vinculado por QR, el número de WhatsApp Cloud o el bot de Telegram.
/// </summary>
public sealed class ChannelReplySender
{
    private readonly IChannelConnectionManager _connections;
    private readonly EvolutionApiClient _evolution;
    private readonly WhatsAppApiClient _whatsAppCloud;
    private readonly TelegramApiClient _telegram;
    private readonly ILogger<ChannelReplySender> _logger;

    public ChannelReplySender(
        IChannelConnectionManager connections,
        EvolutionApiClient evolution,
        WhatsAppApiClient whatsAppCloud,
        TelegramApiClient telegram,
        ILogger<ChannelReplySender> logger)
    {
        _connections = connections;
        _evolution = evolution;
        _whatsAppCloud = whatsAppCloud;
        _telegram = telegram;
        _logger = logger;
    }

    /// <summary>Si la conversación tiene un canal al que se pueda responder desde el panel.</summary>
    public static bool CanReply(Conversation conversation) =>
        conversation.ChannelConnectionId is not null
        && !string.IsNullOrWhiteSpace(conversation.ExternalContactId)
        && conversation.Channel is "WhatsApp" or "Telegram";

    /// <returns>true si el canal aceptó el mensaje.</returns>
    public async Task<bool> SendAsync(Conversation conversation, string text, CancellationToken cancellationToken)
    {
        if (!CanReply(conversation)) return false;

        var connection = await _connections.FindConnectionAsync(conversation.ChannelConnectionId!.Value, cancellationToken);
        if (connection is null || connection.TenantId != conversation.TenantId || !connection.Enabled) return false;
        var to = conversation.ExternalContactId!;
        var mode = connection.ConfigurationMetadata.GetValueOrDefault("ConnectionMode");

        try
        {
            switch (conversation.Channel)
            {
                case "WhatsApp" when mode == "evolution":
                    await _evolution.SendTextAsync(EvolutionInstanceIdentity.InstanceNameFor(connection.TenantId), to, text, cancellationToken);
                    return true;
                case "WhatsApp" when mode == "cloud":
                    var phoneNumberId = connection.ConfigurationMetadata.GetValueOrDefault("PhoneNumberId");
                    if (string.IsNullOrWhiteSpace(phoneNumberId)) return false;
                    return await _whatsAppCloud.SendMessageAsync(
                        phoneNumberId,
                        new WhatsAppCloudSendMessageRequest { To = to, Text = new WhatsAppCloudText { Body = text } },
                        await _connections.GetCredentialsAsync(connection, cancellationToken),
                        cancellationToken);
                case "Telegram":
                    var token = await _connections.GetCredentialsAsync(connection, cancellationToken);
                    return token is not null && await _telegram.SendMessageAsync(
                        new TelegramSendMessageRequest { ChatId = to, Text = text, ParseMode = null }, token, cancellationToken);
                default:
                    return false;
            }
        }
        catch (Exception ex) when (ex is EvolutionApiException or HttpRequestException)
        {
            _logger.LogError(ex, "No se pudo enviar el mensaje del equipo en la conversación {ConversationId}.", conversation.Id);
            return false;
        }
    }
}
