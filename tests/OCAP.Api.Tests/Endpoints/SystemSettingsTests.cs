using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class SystemSettingsTests
{
    private const string AdminEmail = "admin@ocap.io";
    private const string AdminPassword = "ChangeMe_Admin_2026!";
    private const string Url = "/api/system/settings";

    [Fact]
    public async Task TheSystemAdministrator_SavesSettings_AndReadsThemBackWithServerDefaults()
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, AdminEmail, AdminPassword);

        var before = (await admin.GetFromJsonAsync<SettingsResponse>(Url))!;
        before.Settings.WidgetMessagesPerMinute.Should().BeNull("sin tocar el panel rige el servidor");
        before.Defaults.WidgetMessagesPerMinute.Should().Be(15);

        var saved = await admin.PutAsJsonAsync(Url, new
        {
            widgetMessagesPerMinute = 30,
            sessionMinutes = 20,
            allowedOrigins = new[] { " https://Panel.Example.com/ ", "https://panel.example.com" }
        });
        saved.StatusCode.Should().Be(HttpStatusCode.OK, await saved.Content.ReadAsStringAsync());

        var after = (await admin.GetFromJsonAsync<SettingsResponse>(Url))!;
        after.Settings.WidgetMessagesPerMinute.Should().Be(30);
        after.Settings.SessionMinutes.Should().Be(20);
        after.Settings.RequestsPerMinute.Should().BeNull();
        after.Settings.AllowedOrigins.Should().Equal("https://panel.example.com");
    }

    [Theory]
    [InlineData("{\"authAttemptsPerMinute\":1}", "inicio de sesión")]
    [InlineData("{\"requestsPerMinute\":5}", "peticiones")]
    [InlineData("{\"sessionMinutes\":100000}", "sesión")]
    [InlineData("{\"allowedOrigins\":[\"http://sitio.example.com\"]}", "dominio válido")]
    [InlineData("{\"allowedOrigins\":[\"https://sitio.example.com/ruta\"]}", "dominio válido")]
    [InlineData("{\"allowedOrigins\":[\"*\"]}", "dominio válido")]
    public async Task ValuesThatWouldBreakTheInstallation_AreRejectedWithTheReason(string body, string reason)
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, AdminEmail, AdminPassword);

        var response = await admin.PutAsync(Url, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain(reason);
    }

    [Fact]
    public async Task AnotherAdministrator_CannotOpenSystem_NorRemoveWhoManagesIt()
    {
        using var factory = new OcapApiFactory();
        var owner = await LoginAsync(factory, AdminEmail, AdminPassword);
        var second = await InviteAdminAsync(factory, owner, "segundo@ocap.io");

        (await second.GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await second.PutAsJsonAsync(Url, new { rateLimitingEnabled = false })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await factory.CreateClient().GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var members = (await second.GetFromJsonAsync<TeamList>("/api/team"))!;
        var ownerId = members.Members.Single(m => m.Email == AdminEmail).Id;

        var deactivate = await second.PostAsync($"/api/team/{ownerId}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await deactivate.Content.ReadAsStringAsync()).Should().Contain("administra el sistema");

        var demote = await second.PutAsJsonAsync($"/api/team/{ownerId}/role", new { role = "Operator" });
        demote.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await demote.Content.ReadAsStringAsync()).Should().Contain("administra el sistema");
    }

    [Fact]
    public async Task ALoginLimitSetInThePanel_AppliesWithoutRestarting()
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, AdminEmail, AdminPassword);
        (await admin.PutAsJsonAsync(Url, new { authAttemptsPerMinute = 3 })).EnsureSuccessStatusCode();

        var visitor = factory.CreateClient();
        for (var i = 0; i < 3; i++)
        {
            (await Login(visitor, "nadie@ocap.io", "una-contraseña-cualquiera")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await Login(visitor, "nadie@ocap.io", "una-contraseña-cualquiera")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ADomainAddedInThePanel_IsAllowedByCors_AndAnUnknownOneIsNot()
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, AdminEmail, AdminPassword);

        (await AllowedOriginAsync(admin, "https://panel.example.com")).Should().BeNull();

        (await admin.PutAsJsonAsync(Url, new { allowedOrigins = new[] { "https://panel.example.com" } })).EnsureSuccessStatusCode();

        (await AllowedOriginAsync(admin, "https://panel.example.com")).Should().Be("https://panel.example.com");
        (await AllowedOriginAsync(admin, "https://otro.example.com")).Should().BeNull();
    }

    [Fact]
    public async Task TheSessionLengthSetInThePanel_IsUsedForNewTokens()
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, AdminEmail, AdminPassword);
        (await admin.PutAsJsonAsync(Url, new { sessionMinutes = 5 })).EnsureSuccessStatusCode();

        var response = await Login(factory.CreateClient(), AdminEmail, AdminPassword);
        var token = new JwtSecurityTokenHandler().ReadJwtToken((await response.Content.ReadFromJsonAsync<Session>())!.AccessToken);

        (token.ValidTo - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
    }

    private static async Task<string?> AllowedOriginAsync(HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/team");
        request.Headers.Add("Origin", origin);
        var response = await client.SendAsync(request);
        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
    }

    private static async Task<HttpClient> InviteAdminAsync(OcapApiFactory factory, HttpClient owner, string email)
    {
        const string password = "segunda-admin-segura-2026";
        var invite = await owner.PostAsJsonAsync("/api/team/invitations", new { email, fullName = "Segunda", role = "Admin" });
        invite.EnsureSuccessStatusCode();
        var token = (await invite.Content.ReadFromJsonAsync<InviteResponse>())!.Link.Path.Split("token=")[1];
        (await factory.CreateClient().PostAsJsonAsync("/api/auth/password/setup", new { token, newPassword = password })).EnsureSuccessStatusCode();
        return await LoginAsync(factory, email, password);
    }

    private static async Task<HttpClient> LoginAsync(OcapApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await Login(client, email, password);
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<Session>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private sealed record Session(string AccessToken);
    private sealed record Member(Guid Id, string Email);
    private sealed record Link(string Path);
    private sealed record InviteResponse(Member Member, Link Link);
    private sealed record TeamList(List<Member> Members);
    private sealed record Values(int? RequestsPerMinute, int? WidgetMessagesPerMinute, int? SessionMinutes, List<string> AllowedOrigins);
    private sealed record Defaults(int WidgetMessagesPerMinute);
    private sealed record SettingsResponse(Values Settings, Defaults Defaults);
}
