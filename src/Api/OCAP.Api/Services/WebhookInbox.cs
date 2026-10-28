namespace OCAP.Api.Services;

/// <summary>
/// Grupo de InboxMessages con los mensajes de WhatsApp y Telegram ya atendidos. Los proveedores reintentan un webhook
/// si la respuesta tarda: guardarlo en la base evita responder dos veces tras un reinicio o en otra réplica.
/// </summary>
public static class WebhookInbox
{
    public const string ConsumerGroup = "channel-webhook";
}
