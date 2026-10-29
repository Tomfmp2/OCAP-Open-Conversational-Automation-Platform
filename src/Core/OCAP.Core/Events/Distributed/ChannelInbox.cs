namespace OCAP.Core.Events.Distributed;

/// <summary>
/// Grupo de InboxMessages con los mensajes de canales ya atendidos (webhooks de WhatsApp y Telegram, polling de
/// Telegram). Los proveedores reintentan o reentregan: registrarlos en la base evita responder dos veces tras un
/// reinicio o en otra réplica.
/// </summary>
public static class ChannelInbox
{
    public const string ConsumerGroup = "channel-webhook";
}
