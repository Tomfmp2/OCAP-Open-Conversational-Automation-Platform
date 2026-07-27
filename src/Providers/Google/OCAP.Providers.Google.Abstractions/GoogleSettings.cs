namespace OCAP.Providers.Google.Abstractions;

// Configuración de credenciales de aplicación OAuth2 para Google Workspace.
public class GoogleSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
}
