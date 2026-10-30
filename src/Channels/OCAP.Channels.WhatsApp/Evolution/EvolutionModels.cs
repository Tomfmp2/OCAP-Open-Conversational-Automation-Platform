namespace OCAP.Channels.WhatsApp.Evolution;

/// <summary>Estado de una instancia de Evolution API v2 (Baileys): <c>open</c>, <c>connecting</c>, <c>close</c> o <c>refused</c>.</summary>
public static class EvolutionStates
{
    public const string Open = "open";
    public const string Connecting = "connecting";
    public const string Close = "close";
    public const string Refused = "refused";
    /// <summary>La instancia no existe en Evolution (todavía no se vinculó o se borró).</summary>
    public const string Missing = "missing";
}

/// <summary>QR vigente de <c>GET /instance/connect/{instancia}</c> o del evento <c>qrcode.updated</c>.</summary>
public sealed record EvolutionQrCode(string? Base64, string? Code, string? PairingCode);

/// <summary>Resultado de pedir la conexión: o un QR para escanear, o la instancia ya vinculada.</summary>
public sealed record EvolutionConnectResult(string State, EvolutionQrCode? Qr);

/// <summary>Eventos que OCAP pide a Evolution para cada instancia.</summary>
public static class EvolutionEvents
{
    public const string QrCodeUpdated = "QRCODE_UPDATED";
    public const string ConnectionUpdate = "CONNECTION_UPDATE";
    public const string MessagesUpsert = "MESSAGES_UPSERT";

    public static readonly string[] Subscribed = { QrCodeUpdated, ConnectionUpdate, MessagesUpsert };
}

/// <summary>Error de Evolution API con el código HTTP, para que quien llama distinga "no existe" de "falló".</summary>
public sealed class EvolutionApiException : Exception
{
    public int StatusCode { get; }

    public EvolutionApiException(string operation, int statusCode, string body)
        : base($"Evolution API respondió {statusCode} en {operation}.")
    {
        StatusCode = statusCode;
        ResponseBody = body;
    }

    /// <summary>Cuerpo de la respuesta, solo para logs: puede contener datos internos de Evolution.</summary>
    public string ResponseBody { get; }
}
