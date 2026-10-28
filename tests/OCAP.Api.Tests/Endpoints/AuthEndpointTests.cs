using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class AuthEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(OcapApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithBootstrapAdmin_ReturnsAccessTokenAndRefreshCookie()
    {
        var response = await LoginAsync(_client);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("refreshToken", "el refresh token no puede quedar al alcance de JavaScript");
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.Email.Should().Be("admin@ocap.io");
        payload.RoleName.Should().Be("Admin");
        payload.TenantId.Should().NotBe(Guid.Empty);

        var cookie = RefreshCookie(response);
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=strict");
        cookie.Should().ContainEquivalentOf("path=/api/auth");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@ocap.io",
            password = "invalid-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ThenLogout_RotatesAndRevokesTheCookie()
    {
        var login = await LoginAsync(_client);
        var firstCookie = RefreshCookie(login);

        var refresh = await _client.PostAsync("/api/auth/refresh", null);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await refresh.Content.ReadFromJsonAsync<LoginPayload>())!.AccessToken.Should().NotBeNullOrWhiteSpace();
        RefreshCookie(refresh).Should().NotBe(firstCookie);

        (await _client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        (await _client.PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithAnAlreadyRotatedCookie_ClosesTheSession()
    {
        var login = await LoginAsync(_client);
        var stolen = CookieValue(RefreshCookie(login));
        (await _client.PostAsync("/api/auth/refresh", null)).EnsureSuccessStatusCode();

        // Alguien presenta la copia del token anterior: no obtiene sesión y la del usuario también se cierra.
        var attacker = _factory.CreateClient(new() { HandleCookies = false });
        var reuse = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        reuse.Headers.Add("Cookie", $"ocap_refresh={stolen}");
        (await attacker.SendAsync(reuse)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await _client.PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@ocap.io",
            password = "ChangeMe_Admin_2026!"
        });

    private static string RefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("ocap_refresh=", StringComparison.Ordinal));

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0]["ocap_refresh=".Length..];

    private sealed record LoginPayload(
        string AccessToken,
        Guid UserId,
        Guid TenantId,
        string Email,
        string RoleName);
}
