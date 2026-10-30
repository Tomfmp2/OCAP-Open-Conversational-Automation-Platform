using System.Text.Json;
using System.Text.Json.Serialization;
using OCAP.Providers.Google.Abstractions;

namespace OCAP.Api.Services;

public sealed record GoogleTokenGrant(string AccessToken, string? RefreshToken, DateTime ExpiresAtUtc, string? Scope);

/// <summary>Endpoint de tokens de Google: canjea el código de autorización y renueva el access token (dura 1 hora).</summary>
public sealed class GoogleOAuthClient
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(1);

    /// <summary>Permisos que piden las herramientas del asistente si el servidor no indica otros.</summary>
    public static readonly string[] DefaultScopes =
    [
        "https://www.googleapis.com/auth/gmail.send",
        "https://www.googleapis.com/auth/gmail.readonly",
        "https://www.googleapis.com/auth/calendar.events",
        "https://www.googleapis.com/auth/spreadsheets",
        "openid",
        "email"
    ];

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly OrganizationGoogleSettings _organizations;

    public GoogleOAuthClient(HttpClient http, IConfiguration configuration, OrganizationGoogleSettings organizations)
    {
        _http = http;
        _configuration = configuration;
        _organizations = organizations;
    }

    /// <summary>Configuración del servidor (Google:*): respaldo cuando la organización no cargó su cliente OAuth.</summary>
    public GoogleSettings ServerSettings =>
        _configuration.GetSection(GoogleWorkspaceOptions.SectionName).Get<GoogleSettings>() ?? new GoogleSettings();

    /// <summary>Cliente OAuth de la organización o, si no tiene, el del servidor; null si no hay ninguno.</summary>
    public async Task<GoogleSettings?> ResolveAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await _organizations.GetAsync(tenantId, cancellationToken) is { } own)
        {
            var server = ServerSettings;
            return new GoogleSettings { ClientId = own.ClientId, ClientSecret = own.ClientSecret, Scopes = server.Scopes };
        }
        var fallback = ServerSettings;
        return !string.IsNullOrWhiteSpace(fallback.ClientId) && !string.IsNullOrWhiteSpace(fallback.ClientSecret) ? fallback : null;
    }

    public async Task<bool> IsConfiguredAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await ResolveAsync(tenantId, cancellationToken) is not null;

    public Task<GoogleTokenGrant> ExchangeCodeAsync(Guid tenantId, string authCode, string redirectUri, CancellationToken cancellationToken) =>
        RequestAsync(tenantId, new Dictionary<string, string>
        {
            ["code"] = authCode,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        }, cancellationToken);

    public Task<GoogleTokenGrant> RefreshAsync(Guid tenantId, string refreshToken, CancellationToken cancellationToken) =>
        RequestAsync(tenantId, new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        }, cancellationToken);

    private async Task<GoogleTokenGrant> RequestAsync(Guid tenantId, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var settings = await ResolveAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Google no está configurado para esta organización.");
        form["client_id"] = settings.ClientId;
        form["client_secret"] = settings.ClientSecret;

        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync(TokenEndpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // El cuerpo puede repetir datos de la petición: solo el código de estado llega a los logs.
            throw new HttpRequestException($"Google token endpoint respondió {(int)response.StatusCode}.", null, response.StatusCode);
        }

        var parsed = await JsonSerializer.DeserializeAsync<TokenResponse>(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(parsed?.AccessToken))
            throw new HttpRequestException("Google no devolvió un access_token válido.");

        var lifetime = parsed.ExpiresIn > 0 ? TimeSpan.FromSeconds(parsed.ExpiresIn) : DefaultLifetime;
        return new GoogleTokenGrant(parsed.AccessToken, parsed.RefreshToken, DateTime.UtcNow.Add(lifetime), parsed.Scope);
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope);
}
