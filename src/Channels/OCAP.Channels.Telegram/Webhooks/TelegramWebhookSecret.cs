using System.Security.Cryptography;
using System.Text;

namespace OCAP.Channels.Telegram.Webhooks;

/// <summary>
/// Secreto del webhook de un bot registrado desde el panel, derivado de su token y del id de la conexión: Telegram lo
/// devuelve en <c>X-Telegram-Bot-Api-Secret-Token</c> y OCAP lo recalcula, sin guardar ni configurar nada.
/// Solo quien conoce el token del bot puede producirlo.
/// </summary>
public static class TelegramWebhookSecret
{
    public static string For(string botToken, Guid connectionId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(botToken));
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes($"ocap-telegram-webhook:{connectionId:N}")));
    }

    public static bool IsValid(string botToken, Guid connectionId, string? received)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(received)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(For(botToken, connectionId)),
            Encoding.ASCII.GetBytes(received.Trim()));
    }
}
