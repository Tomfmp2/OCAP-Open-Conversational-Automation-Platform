using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Infrastructure.Services.Providers;

// Proveedor de autenticación OAuth2/OIDC para Google (CAP-15).
public class GoogleExternalAuthProvider : IExternalAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExternalProviderSettings _settings;

    public string ProviderName => "google";
    public string DisplayName => string.IsNullOrWhiteSpace(_settings.DisplayName) ? "Google" : _settings.DisplayName;
    public bool IsEnabled => _settings.IsEnabled && !string.IsNullOrWhiteSpace(_settings.ClientId);

    public GoogleExternalAuthProvider(HttpClient httpClient, IOptions<ExternalAuthenticationSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var settings = options.Value ?? new ExternalAuthenticationSettings();
        _settings = settings.ExternalProviders.TryGetValue("Google", out var s) ? s : new ExternalProviderSettings();
    }

    public string BuildAuthorizationUrl(string redirectUri, string state)
    {
        var scope = Uri.EscapeDataString(_settings.Scope ?? "openid profile email");
        var encodedUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={_settings.ClientId}&redirect_uri={encodedUri}&scope={scope}&state={encodedState}&access_type=online";
    }

    public async Task<ExternalUserPayloadDto?> ProcessCallbackAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(code)) return null;

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        var tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);
        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken)) return null;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);

        var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode) return null;

        var userInfo = await userResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken);
        if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Sub)) return null;

        var claims = new Dictionary<string, string>
        {
            ["sub"] = userInfo.Sub,
            ["email"] = userInfo.Email ?? string.Empty,
            ["name"] = userInfo.Name ?? string.Empty
        };

        return new ExternalUserPayloadDto(
            ProviderName,
            userInfo.Sub,
            userInfo.Email ?? $"{userInfo.Sub}@google.external",
            userInfo.Name ?? "Usuario Google",
            userInfo.Picture,
            claims
        );
    }

    private record GoogleTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record GoogleUserInfoResponse(
        [property: JsonPropertyName("sub")] string Sub,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("picture")] string? Picture
    );
}
