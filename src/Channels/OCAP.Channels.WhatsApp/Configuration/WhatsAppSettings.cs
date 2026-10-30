namespace OCAP.Channels.WhatsApp.Configuration;

/// <summary>
/// Configuración del canal WhatsApp. Provider=Evolution usa QR (Baileys);
/// Provider=Cloud usa Meta Graph API.
/// </summary>
public class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    public bool Enabled { get; set; } = true;

    /// <summary>Evolution | Cloud</summary>
    public string Provider { get; set; } = "Evolution";

    // --- Evolution API ---
    /// <summary>URL de Evolution vista desde la API (en Docker: http://evolution-api:8080).</summary>
    public string BaseUrl { get; set; } = string.Empty;
    /// <summary>API key global de Evolution (AUTHENTICATION_API_KEY).</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>
    /// Opcional. Secreto con el que se firma el webhook de cada instancia; si falta se deriva de <see cref="ApiKey"/>,
    /// así que no hay que configurar nada más. Ver <see cref="EffectiveWebhookSecret"/>.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
    /// <summary>URL del webhook de OCAP vista desde Evolution (en Docker: http://ocap-api:5000/api/webhooks/whatsapp).</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    // --- Meta Cloud API (opcional) ---
    public string ApiToken { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;

    /// <summary>
    /// Clave HMAC de las firmas del webhook: <see cref="WebhookSecret"/> si se definió; si no, una derivada de la API
    /// key de Evolution (con un prefijo propio, para no usar la misma clave tal cual en dos protocolos). Vacía solo si
    /// tampoco hay API key, y entonces el webhook rechaza todo.
    /// </summary>
    public string EffectiveWebhookSecret =>
        !string.IsNullOrWhiteSpace(WebhookSecret) ? WebhookSecret
        : string.IsNullOrWhiteSpace(ApiKey) ? string.Empty
        : $"ocap-whatsapp-webhook:{ApiKey}";

    public bool IsEvolution =>
        string.Equals(Provider, "Evolution", StringComparison.OrdinalIgnoreCase);
}
