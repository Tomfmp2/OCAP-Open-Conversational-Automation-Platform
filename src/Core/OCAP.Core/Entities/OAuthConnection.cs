namespace OCAP.Core.Entities;

// Entidad de persistencia que representa la conexión de autenticación OAuth2 de un usuario con proveedores externos.
public class OAuthConnection
{
    public Guid Id { private set; get; }
    public Guid UserId { private set; get; }
    public string Provider { private set; get; } = string.Empty;
    public string AccessToken { private set; get; } = string.Empty;
    public string RefreshToken { private set; get; } = string.Empty;
    public DateTime TokenExpiration { private set; get; }
    public string Scopes { private set; get; } = string.Empty;
    public DateTime UpdatedAt { private set; get; }

    private OAuthConnection() { } // Constructor ORM

    public OAuthConnection(
        Guid id,
        Guid userId,
        string provider,
        string accessToken,
        string refreshToken,
        DateTime tokenExpiration,
        string scopes)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de conexión no puede ser vacío.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("El ID de usuario no puede ser vacío.", nameof(userId));

        Id = id;
        UserId = userId;
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        AccessToken = accessToken ?? string.Empty;
        RefreshToken = refreshToken ?? string.Empty;
        TokenExpiration = tokenExpiration;
        Scopes = scopes ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTokens(string accessToken, string refreshToken, DateTime expiration)
    {
        AccessToken = accessToken ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(refreshToken)) RefreshToken = refreshToken;
        TokenExpiration = expiration;
        UpdatedAt = DateTime.UtcNow;
    }
}
