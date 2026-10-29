using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Core.Entities;
using OCAP.Core.ValueObjects;
using OCAP.Infrastructure.BackgroundJobs;
using OCAP.Infrastructure.Options;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Tests.Endpoints;

public class OrganizationPreferencesTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public OrganizationPreferencesTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Preferences_AreValidated_AndSaved()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        object Prefs(string? language = "en", string? timeZone = "America/Bogota", string? panelUrl = "https://panel.tienda.com", int? days = 90) =>
            new { language, timeZone, panelUrl, conversationRetentionDays = days };

        (await admin.PutAsJsonAsync("/api/settings/preferences", Prefs(language: "fr"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/settings/preferences", Prefs(timeZone: "Marte/Olympus"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/settings/preferences", Prefs(panelUrl: "http://panel.tienda.com"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/settings/preferences", Prefs(days: 1))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await admin.PutAsJsonAsync("/api/settings/preferences", Prefs())).StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = JsonDocument.Parse(await admin.GetStringAsync("/api/settings/preferences")).RootElement;
        saved.GetProperty("timeZone").GetString().Should().Be("America/Bogota");
        saved.GetProperty("conversationRetentionDays").GetInt32().Should().Be(90);
    }

    [Fact]
    public async Task ChangingThePanelUrl_RequiresTheSecurityPermission()
    {
        // La dirección del panel va en los enlaces con tokens de contraseña.
        var token = TestAuthHelper.CreateAccessToken(_factory, "ajustes2@ocap.test", "Settings", ["Settings.Manage"]);
        var settingsOnly = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

        (await settingsOnly.PutAsJsonAsync("/api/settings/preferences", new { panelUrl = "https://otro-dominio.com" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GoogleClient_IsStoredWithoutReturningTheSecret_AndTheConsentUrlCarriesASignedState()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        (await admin.PutAsJsonAsync("/api/settings/google", new { clientId = "no-es-de-google", clientSecret = "x" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/settings/google", new { clientId = "123-abc.apps.googleusercontent.com", clientSecret = "GOCSPX-secreto" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var raw = await admin.GetStringAsync("/api/settings/google");
        raw.Should().NotContain("GOCSPX-secreto").And.Contain("123-abc.apps.googleusercontent.com");

        var consent = JsonDocument.Parse(await admin.GetStringAsync(
            "/api/integrations/google/authorize-url?redirectUri=https%3A%2F%2Fpanel.tienda.com%2Fintegrations%2Fgoogle%2Fcallback")).RootElement.GetProperty("url").GetString()!;
        consent.Should().StartWith("https://accounts.google.com/").And.Contain("client_id=123-abc.apps.googleusercontent.com").And.Contain("state=");

        // Un código sin el state que emitió la API (otra cuenta, otro navegador) se rechaza antes de llamar a Google.
        (await admin.PostAsJsonAsync("/api/integrations/google/connect", new { authCode = "codigo", redirectUri = "https://panel.tienda.com/x", state = "falso" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Retention_RemovesCustomerMessagesOlderThanTheChosenDays()
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        (await admin.PutAsJsonAsync("/api/settings/preferences", new { conversationRetentionDays = 30 })).EnsureSuccessStatusCode();

        Guid oldId, recentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            var contact = new User(Guid.NewGuid(), "Cliente", tenantId);
            var conversation = new Conversation(Guid.NewGuid(), contact.Id, tenantId);
            var old = new Message(Guid.NewGuid(), conversation.Id, new MessageContent("de hace dos meses"), SenderType.User, tenantId);
            var recent = new Message(Guid.NewGuid(), conversation.Id, new MessageContent("de hoy"), SenderType.User, tenantId);
            db.AddRange(contact, conversation, old, recent);
            db.Entry(old).Property(m => m.CreatedAt).CurrentValue = DateTime.UtcNow.AddDays(-60);
            await db.SaveChangesAsync();
            (oldId, recentId) = (old.Id, recent.Id);
        }

        var job = _factory.Services.GetServices<IHostedService>().OfType<AuditAndOutboxRetentionBackgroundService>().Single();
        await job.PerformRetentionPurgeAsync(new RetentionOptions(), CancellationToken.None);

        using var check = _factory.Services.CreateScope();
        var messages = check.ServiceProvider.GetRequiredService<OCAPDbContext>().Messages.IgnoreQueryFilters();
        (await messages.AnyAsync(m => m.Id == oldId)).Should().BeFalse();
        (await messages.AnyAsync(m => m.Id == recentId)).Should().BeTrue();
    }
}
