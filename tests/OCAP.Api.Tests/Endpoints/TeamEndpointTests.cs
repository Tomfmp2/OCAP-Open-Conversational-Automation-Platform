using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class TeamEndpointTests
{
    private const string AdminPassword = "ChangeMe_Admin_2026!";

    [Fact]
    public async Task AnInvitedOperator_SetsAPassword_UsesTheInbox_ButCannotManageTheTeam()
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, "admin@ocap.io", AdminPassword);

        var invite = await admin.PostAsJsonAsync("/api/team/invitations", new { email = "Ana@Ocap.io", fullName = "Ana", role = "Operator" });
        invite.StatusCode.Should().Be(HttpStatusCode.OK, await invite.Content.ReadAsStringAsync());
        var invited = (await invite.Content.ReadFromJsonAsync<InviteResponse>())!;
        invited.Member.InvitationPending.Should().BeTrue();
        invited.Link.Emailed.Should().BeFalse("las pruebas no tienen SMTP");
        var token = invited.Link.Path.Split("token=")[1];

        var anonymous = factory.CreateClient();
        (await anonymous.PostAsJsonAsync("/api/auth/password/setup", new { token, newPassword = "corta1" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await anonymous.PostAsJsonAsync("/api/auth/password/setup", new { token, newPassword = "operadora-segura-2026" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await anonymous.PostAsJsonAsync("/api/auth/password/setup", new { token, newPassword = "otra-clave-segura-2026" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "el enlace es de un solo uso");

        var operatorClient = await LoginAsync(factory, "ana@ocap.io", "operadora-segura-2026");
        (await operatorClient.GetAsync("/api/inbox")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await operatorClient.GetAsync("/api/team")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Desactivada, ya no entra.
        (await admin.PostAsync($"/api/team/{invited.Member.Id}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Login(anonymous, "ana@ocap.io", "operadora-segura-2026")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheLastAdministrator_CannotBeRemoved_AndEmailsAreUnique()
    {
        using var factory = new OcapApiFactory();
        var admin = await LoginAsync(factory, "admin@ocap.io", AdminPassword);
        var members = (await admin.GetFromJsonAsync<TeamList>("/api/team"))!;
        var me = members.Members.Single(m => m.Email == "admin@ocap.io");

        var deactivate = await admin.PostAsync($"/api/team/{me.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await deactivate.Content.ReadAsStringAsync()).Should().Contain("propia cuenta");

        var demote = await admin.PutAsJsonAsync($"/api/team/{me.Id}/role", new { role = "Operator" });
        demote.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await demote.Content.ReadAsStringAsync()).Should().Contain("propia cuenta");

        var duplicate = await admin.PostAsJsonAsync("/api/team/invitations", new { email = "admin@ocap.io", fullName = "Otra", role = "Admin" });
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await duplicate.Content.ReadAsStringAsync()).Should().Contain("Ya existe una cuenta");
    }

    [Fact]
    public async Task ForgotPassword_AnswersTheSame_WithoutRevealingAccounts()
    {
        using var factory = new OcapApiFactory();
        var client = factory.CreateClient();

        var known = await client.PostAsJsonAsync("/api/auth/password/forgot", new { email = "admin@ocap.io" });
        var unknown = await client.PostAsJsonAsync("/api/auth/password/forgot", new { email = "nadie@ocap.io" });

        known.StatusCode.Should().Be(HttpStatusCode.OK);
        (await known.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync());
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
    private sealed record Member(Guid Id, string Email, bool InvitationPending);
    private sealed record Link(string Path, bool Emailed);
    private sealed record InviteResponse(Member Member, Link Link);
    private sealed record TeamList(List<Member> Members, bool EmailConfigured);
}
