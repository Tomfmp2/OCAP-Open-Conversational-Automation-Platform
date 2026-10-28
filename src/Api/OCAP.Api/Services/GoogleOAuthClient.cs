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

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public GoogleOAuthClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public GoogleSettings Settings =>
        _configuration.GetSection(GoogleWorkspaceOptions.SectionName).Get<GoogleSettings>() ?? new GoogleSettings();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Settings.ClientId) && !string.IsNullOrWhiteSpace(Settings.ClientSecret);

    public Task<GoogleTokenGrant> ExchangeCodeAsync(string authCode, string redirectUri, CancellationToken cancellationToken) =>
        RequestAsync(new Dictionary<string, string>
        {
            ["code"] = authCode,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        }, cancellationToken);

    public Task<GoogleTokenGrant> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        RequestAsync(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        }, cancellationToken);

    private async Task<GoogleTokenGrant> RequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var settings = Settings;
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
