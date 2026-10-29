using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class OrganizationEmailSettingsTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public OrganizationEmailSettingsTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Smtp_IsConfiguredFromThePanel_WithoutEverReturningThePassword()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        object Smtp(string host, string? password) => new { host, port = 587, username = "envios", password, from = "envios@tienda.com", useSsl = true };

        (await admin.PutAsJsonAsync("/api/settings/email", Smtp("127.0.0.1", "secreta"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/settings/email", new { host = "8.8.8.8", port = 22, from = "a@b.com", useSsl = true }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/settings/email", Smtp("8.8.8.8", "secreta"))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var raw = await admin.GetStringAsync("/api/settings/email");
        raw.Should().NotContain("secreta");
        var settings = JsonDocument.Parse(raw).RootElement.GetProperty("settings");
        settings.GetProperty("host").GetString().Should().Be("8.8.8.8");
        settings.GetProperty("hasPassword").GetBoolean().Should().BeTrue();

        // Sin contraseña en la petición se conserva la guardada.
        (await admin.PutAsJsonAsync("/api/settings/email", Smtp("8.8.4.4", null))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        JsonDocument.Parse(await admin.GetStringAsync("/api/settings/email")).RootElement
            .GetProperty("settings").GetProperty("hasPassword").GetBoolean().Should().BeTrue();
        JsonDocument.Parse(await admin.GetStringAsync("/api/team")).RootElement.GetProperty("emailConfigured").GetBoolean().Should().BeTrue();

        (await admin.DeleteAsync("/api/settings/email")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        JsonDocument.Parse(await admin.GetStringAsync("/api/settings/email")).RootElement
            .GetProperty("settings").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task OnlyWhoManagesSecurity_CanChangeTheSmtp()
    {
        // Ajustes generales no bastan: el SMTP recibe los enlaces de recuperación de contraseña.
        var token = TestAuthHelper.CreateAccessToken(_factory, "ajustes@ocap.test", "Settings", ["Settings.Manage"]);
        var operatorClient = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

        (await operatorClient.GetAsync("/api/settings/email")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await operatorClient.PutAsJsonAsync("/api/settings/email", new { host = "8.8.8.8", port = 587, from = "a@b.com", useSsl = true }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
