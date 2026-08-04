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
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string Instance { get; set; } = "ocap-main";
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;

    // --- Meta Cloud API (opcional) ---
    public string ApiToken { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;

    public bool IsEvolution =>
        string.Equals(Provider, "Evolution", StringComparison.OrdinalIgnoreCase);
}
