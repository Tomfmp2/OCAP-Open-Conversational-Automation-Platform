namespace OCAP.Providers.Google.Abstractions;

// Objeto de credencial OAuth2 seguro para la autenticación con APIs de Google.
public class OAuthCredential
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public List<string> Scopes { get; set; } = new();

    // Evalúa si el Token de acceso ha expirado.
    public bool IsExpired => DateTime.UtcNow >= TokenExpiration;
}
