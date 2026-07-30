using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class AuthEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(OcapApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithBootstrapAdmin_ReturnsTokens()
    {
        var response = await LoginAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.Email.Should().Be("admin@ocap.io");
        payload.RoleName.Should().Be("Admin");
        payload.TenantId.Should().NotBe(Guid.Empty);
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
    public async Task Refresh_ThenLogout_RotatesAndRevokesToken()
    {
        var login = await LoginAsync();
        var loginPayload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        loginPayload.Should().NotBeNull();

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = loginPayload!.RefreshToken
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await refresh.Content.ReadFromJsonAsync<LoginPayload>();
        refreshed.Should().NotBeNull();
        refreshed!.RefreshToken.Should().NotBe(loginPayload.RefreshToken);

        var logout = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = refreshed.RefreshToken
        });
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = refreshed.RefreshToken
        });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private Task<HttpResponseMessage> LoginAsync() =>
        _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@ocap.io",
            password = "ChangeMe_Admin_2026!"
        });

    private sealed record LoginPayload(
        string AccessToken,
        string RefreshToken,
        Guid UserId,
        Guid TenantId,
        string Email,
        string RoleName);
}
