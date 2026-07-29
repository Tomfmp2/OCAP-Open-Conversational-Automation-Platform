using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Infrastructure.Services.Providers;

// Proveedor de autenticación OpenID Connect genérico para SSO corporativo (CAP-15).
public class GenericOidcExternalAuthProvider : IExternalAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExternalProviderSettings _settings;

    public string ProviderName => "oidc";
    public string DisplayName => string.IsNullOrWhiteSpace(_settings.DisplayName) ? "Enterprise Single Sign-On" : _settings.DisplayName;
    public bool IsEnabled => _settings.IsEnabled && !string.IsNullOrWhiteSpace(_settings.ClientId) && !string.IsNullOrWhiteSpace(_settings.Authority);

    public GenericOidcExternalAuthProvider(HttpClient httpClient, IOptions<ExternalAuthenticationSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var settings = options.Value ?? new ExternalAuthenticationSettings();
        _settings = settings.ExternalProviders.TryGetValue("GenericOidc", out var s) ? s : new ExternalProviderSettings();
    }

    public string BuildAuthorizationUrl(string redirectUri, string state)
    {
        var authority = (_settings.Authority ?? string.Empty).TrimEnd('/');
        var scope = Uri.EscapeDataString(_settings.Scope ?? "openid profile email");
        var encodedUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"{authority}/protocol/openid-connect/auth?response_type=code&client_id={_settings.ClientId}&redirect_uri={encodedUri}&scope={scope}&state={encodedState}";
    }

    public async Task<ExternalUserPayloadDto?> ProcessCallbackAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(code)) return null;

        var authority = (_settings.Authority ?? string.Empty).TrimEnd('/');
        var tokenEndpoint = $"{authority}/protocol/openid-connect/token";
        var userinfoEndpoint = $"{authority}/protocol/openid-connect/userinfo";

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        var tokenResponse = await _httpClient.PostAsync(tokenEndpoint, tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<OidcTokenResponse>(cancellationToken);
        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken)) return null;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, userinfoEndpoint);
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);

        var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode) return null;

        var userInfo = await userResponse.Content.ReadFromJsonAsync<OidcUserInfoResponse>(cancellationToken);
        if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Sub)) return null;

        var email = userInfo.Email ?? $"{userInfo.Sub}@oidc.external";

        var claims = new Dictionary<string, string>
        {
            ["sub"] = userInfo.Sub,
            ["email"] = email,
            ["name"] = userInfo.Name ?? userInfo.PreferredUsername ?? string.Empty
        };

        return new ExternalUserPayloadDto(
            ProviderName,
            userInfo.Sub,
            email,
            userInfo.Name ?? userInfo.PreferredUsername ?? "Usuario SSO",
            userInfo.Picture,
            claims
        );
    }

    private record OidcTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record OidcUserInfoResponse(
        [property: JsonPropertyName("sub")] string Sub,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("preferred_username")] string? PreferredUsername,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("picture")] string? Picture
    );
}
