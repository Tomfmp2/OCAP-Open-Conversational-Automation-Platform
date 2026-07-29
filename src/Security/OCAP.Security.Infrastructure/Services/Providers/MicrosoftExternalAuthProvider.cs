using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Infrastructure.Services.Providers;

// Proveedor de autenticación OAuth2/OIDC para Microsoft Entra ID (Azure AD / Microsoft 365) (CAP-15).
public class MicrosoftExternalAuthProvider : IExternalAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExternalProviderSettings _settings;

    public string ProviderName => "microsoft";
    public string DisplayName => string.IsNullOrWhiteSpace(_settings.DisplayName) ? "Microsoft" : _settings.DisplayName;
    public bool IsEnabled => _settings.IsEnabled && !string.IsNullOrWhiteSpace(_settings.ClientId);

    public MicrosoftExternalAuthProvider(HttpClient httpClient, IOptions<ExternalAuthenticationSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var settings = options.Value ?? new ExternalAuthenticationSettings();
        _settings = settings.ExternalProviders.TryGetValue("Microsoft", out var s) ? s : new ExternalProviderSettings();
    }

    public string BuildAuthorizationUrl(string redirectUri, string state)
    {
        var tenant = string.IsNullOrWhiteSpace(_settings.TenantId) ? "common" : _settings.TenantId;
        var scope = Uri.EscapeDataString(_settings.Scope ?? "openid profile email User.Read");
        var encodedUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize?response_type=code&client_id={_settings.ClientId}&redirect_uri={encodedUri}&scope={scope}&state={encodedState}&response_mode=query";
    }

    public async Task<ExternalUserPayloadDto?> ProcessCallbackAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(code)) return null;

        var tenant = string.IsNullOrWhiteSpace(_settings.TenantId) ? "common" : _settings.TenantId;
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        var tokenResponse = await _httpClient.PostAsync($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token", tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(cancellationToken);
        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken)) return null;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);

        var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode) return null;

        var userInfo = await userResponse.Content.ReadFromJsonAsync<MicrosoftUserInfoResponse>(cancellationToken);
        if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Id)) return null;

        var email = userInfo.Mail ?? userInfo.UserPrincipalName ?? $"{userInfo.Id}@microsoft.external";

        var claims = new Dictionary<string, string>
        {
            ["sub"] = userInfo.Id,
            ["email"] = email,
            ["name"] = userInfo.DisplayName ?? string.Empty
        };

        return new ExternalUserPayloadDto(
            ProviderName,
            userInfo.Id,
            email,
            userInfo.DisplayName ?? "Usuario Microsoft",
            null,
            claims
        );
    }

    private record MicrosoftTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record MicrosoftUserInfoResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("mail")] string? Mail,
        [property: JsonPropertyName("userPrincipalName")] string? UserPrincipalName
    );
}
