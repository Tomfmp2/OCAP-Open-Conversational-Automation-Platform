using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Infrastructure.Services.Providers;

// Proveedor de autenticación OAuth2 para GitHub (CAP-15).
public class GitHubExternalAuthProvider : IExternalAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExternalProviderSettings _settings;

    public string ProviderName => "github";
    public string DisplayName => string.IsNullOrWhiteSpace(_settings.DisplayName) ? "GitHub" : _settings.DisplayName;
    public bool IsEnabled => _settings.IsEnabled && !string.IsNullOrWhiteSpace(_settings.ClientId);

    public GitHubExternalAuthProvider(HttpClient httpClient, IOptions<ExternalAuthenticationSettings> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var settings = options.Value ?? new ExternalAuthenticationSettings();
        _settings = settings.ExternalProviders.TryGetValue("GitHub", out var s) ? s : new ExternalProviderSettings();
    }

    public string BuildAuthorizationUrl(string redirectUri, string state)
    {
        var scope = Uri.EscapeDataString(_settings.Scope ?? "read:user user:email");
        var encodedUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"https://github.com/login/oauth/authorize?client_id={_settings.ClientId}&redirect_uri={encodedUri}&scope={scope}&state={encodedState}";
    }

    public async Task<ExternalUserPayloadDto?> ProcessCallbackAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(code)) return null;

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var requestMsg = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = tokenRequest
        };
        requestMsg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var tokenResponse = await _httpClient.SendAsync(requestMsg, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken);
        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken)) return null;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
        userRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("OCAP-Platform", "1.0"));

        var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode) return null;

        var userInfo = await userResponse.Content.ReadFromJsonAsync<GitHubUserInfoResponse>(cancellationToken);
        if (userInfo == null || userInfo.Id == 0) return null;

        var externalId = userInfo.Id.ToString();
        var email = userInfo.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
            emailsRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("OCAP-Platform", "1.0"));

            var emailsResponse = await _httpClient.SendAsync(emailsRequest, cancellationToken);
            if (emailsResponse.IsSuccessStatusCode)
            {
                var emails = await emailsResponse.Content.ReadFromJsonAsync<List<GitHubEmailResponse>>(cancellationToken);
                var primary = emails?.FirstOrDefault(e => e.Primary && e.Verified) ?? emails?.FirstOrDefault();
                if (primary != null) email = primary.Email;
            }
        }

        email ??= $"{userInfo.Login}@github.external";

        var claims = new Dictionary<string, string>
        {
            ["sub"] = externalId,
            ["email"] = email,
            ["name"] = userInfo.Name ?? userInfo.Login,
            ["login"] = userInfo.Login
        };

        return new ExternalUserPayloadDto(
            ProviderName,
            externalId,
            email,
            userInfo.Name ?? userInfo.Login,
            userInfo.AvatarUrl,
            claims
        );
    }

    private record GitHubTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record GitHubUserInfoResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl
    );
    private record GitHubEmailResponse(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("primary")] bool Primary,
        [property: JsonPropertyName("verified")] bool Verified
    );
}
