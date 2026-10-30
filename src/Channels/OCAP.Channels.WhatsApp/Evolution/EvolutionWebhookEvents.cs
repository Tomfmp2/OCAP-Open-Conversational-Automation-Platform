using System.Text.Json;

namespace OCAP.Channels.WhatsApp.Evolution;

/// <summary>Evento del webhook de Evolution v2 ya interpretado.</summary>
public abstract record EvolutionWebhookEvent(string Instance);

public sealed record EvolutionQrUpdatedEvent(string Instance, EvolutionQrCode Qr) : EvolutionWebhookEvent(Instance);

/// <summary>Cambio de estado de la instancia (open, connecting, close o refused); con open trae el número vinculado.</summary>
public sealed record EvolutionConnectionUpdatedEvent(string Instance, string State, string? PhoneNumber, string? ProfileName)
    : EvolutionWebhookEvent(Instance);

/// <summary>
/// Texto de un contacto. <c>From</c> es su número (solo dígitos); <c>MessageId</c> es el id de WhatsApp, que sirve
/// para no responder dos veces cuando Evolution reintenta el webhook.
/// </summary>
public sealed record EvolutionTextMessageEvent(string Instance, string MessageId, string From, string SenderName, string Text)
    : EvolutionWebhookEvent(Instance);

/// <summary>
/// Lee el sobre <c>{ event, instance, data }</c> que envía Evolution v2. Devuelve <c>null</c> para lo que OCAP
/// no procesa: mensajes propios, de grupos, estados, canales, sin texto o eventos no suscritos.
/// </summary>
public static class EvolutionWebhookParser
{
    /// <summary>Nombre de la instancia que envía el webhook, para verificar su firma antes de interpretar nada.</summary>
    public static string? ReadInstance(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Str(doc.RootElement, "instance");
    }

    public static EvolutionWebhookEvent? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return null;

        var instance = Str(root, "instance");
        if (string.IsNullOrWhiteSpace(instance) || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return null;

        // Evolution envía "messages.upsert"; con webhook por eventos o versiones anteriores, "MESSAGES_UPSERT".
        var eventName = (Str(root, "event") ?? string.Empty).Replace('_', '.').ToLowerInvariant();
        return eventName switch
        {
            "qrcode.updated" => ParseQr(instance, data),
            "connection.update" => ParseConnection(instance, data),
            "messages.upsert" => ParseMessage(instance, data),
            _ => null
        };
    }

    private static EvolutionQrUpdatedEvent? ParseQr(string instance, JsonElement data)
    {
        if (!data.TryGetProperty("qrcode", out var qr) || qr.ValueKind != JsonValueKind.Object) return null;
        return new EvolutionQrUpdatedEvent(instance, new EvolutionQrCode(Str(qr, "base64"), Str(qr, "code"), Str(qr, "pairingCode")));
    }

    private static EvolutionConnectionUpdatedEvent? ParseConnection(string instance, JsonElement data)
    {
        var state = Str(data, "state");
        if (string.IsNullOrWhiteSpace(state)) return null;
        // wuid llega como "573001234567@s.whatsapp.net".
        var phone = Digits(Str(data, "wuid"));
        return new EvolutionConnectionUpdatedEvent(instance, state.ToLowerInvariant(), phone, Str(data, "profileName"));
    }

    private static EvolutionTextMessageEvent? ParseMessage(string instance, JsonElement data)
    {
        if (!data.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.Object) return null;
        if (key.TryGetProperty("fromMe", out var fromMe) && fromMe.ValueKind == JsonValueKind.True) return null;

        var remoteJid = Str(key, "remoteJid") ?? string.Empty;
        var messageId = Str(key, "id");
        var from = IndividualSender(remoteJid, Str(key, "remoteJidAlt") ?? Str(key, "senderPn"));
        var text = data.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object
            ? Str(message, "conversation") ?? (message.TryGetProperty("extendedTextMessage", out var ext) ? Str(ext, "text") : null)
            : null;

        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(text)) return null;
        return new EvolutionTextMessageEvent(instance, messageId, from, Str(data, "pushName") ?? string.Empty, text.Trim());
    }

    /// <summary>
    /// A quién responder en un chat individual; null para grupos, estados (status@broadcast), canales y difusiones.
    /// WhatsApp identifica cada vez más contactos con un LID ("123@lid") y manda el número real aparte (remoteJidAlt):
    /// se prefiere el número; sin él, se responde al propio LID, que Evolution también acepta.
    /// </summary>
    private static string? IndividualSender(string remoteJid, string? alternativeJid)
    {
        if (remoteJid.EndsWith("@s.whatsapp.net", StringComparison.Ordinal)) return Digits(remoteJid);
        if (!remoteJid.EndsWith("@lid", StringComparison.Ordinal)) return null;
        return alternativeJid?.EndsWith("@s.whatsapp.net", StringComparison.Ordinal) == true ? Digits(alternativeJid) : remoteJid;
    }

    private static string? Str(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? Digits(string? jid)
    {
        if (string.IsNullOrWhiteSpace(jid)) return null;
        var user = jid.Split('@')[0].Split(':')[0];
        var digits = new string(user.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }
}
