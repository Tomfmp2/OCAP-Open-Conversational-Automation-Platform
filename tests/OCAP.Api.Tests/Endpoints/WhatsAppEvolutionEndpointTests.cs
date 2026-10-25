using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;

namespace OCAP.Api.Tests.Endpoints;

public class WhatsAppEvolutionFactory : OcapApiFactory
{
    private const string EvolutionApiKey = "evolution-global-key";

    /// <summary>Sin WhatsApp:WebhookSecret configurado: la clave de las firmas se deriva de la API key.</summary>
    public static string WebhookSecret => new WhatsAppSettings { ApiKey = EvolutionApiKey }.EffectiveWebhookSecret;

    public FakeEvolutionApi Evolution { get; } = new();

    protected virtual string ApiKey => EvolutionApiKey;

    protected override IDictionary<string, string?> ExtraSettings => new Dictionary<string, string?>
    {
        ["WhatsApp:BaseUrl"] = "http://evolution-api:8080",
        ["WhatsApp:ApiKey"] = ApiKey,
        ["WhatsApp:WebhookUrl"] = "http://ocap-api:5000/api/webhooks/whatsapp"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.AddHttpClient<EvolutionApiClient>().ConfigurePrimaryHttpMessageHandler(() => Evolution));
    }
}

/// <summary>Instalación sin API key de Evolution: no hay clave para verificar firmas y el webhook rechaza todo.</summary>
public sealed class WhatsAppWithoutSecretFactory : WhatsAppEvolutionFactory
{
    protected override string ApiKey => string.Empty;
}

public class WhatsAppEvolutionEndpointTests : IClassFixture<WhatsAppEvolutionFactory>
{
    private static readonly Guid SeedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly string SeedInstance = EvolutionInstanceIdentity.InstanceNameFor(SeedTenantId);
    private readonly WhatsAppEvolutionFactory _factory;

    public WhatsAppEvolutionEndpointTests(WhatsAppEvolutionFactory factory) => _factory = factory;

    [Fact]
    public async Task Link_RequiresAuthentication()
    {
        var response = await _factory.CreateClient().PostAsync("/api/channels/whatsapp/link", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_LinkScanReceiveAndReply()
    {
        var admin = await AdminClientAsync();

        // 1. Vincular: crea la instancia de la organización con su webhook firmado y devuelve el QR.
        var link = await admin.PostAsync("/api/channels/whatsapp/link", null);
        link.StatusCode.Should().Be(HttpStatusCode.OK);
        var pending = await link.Content.ReadFromJsonAsync<LinkStatus>();
        pending!.State.Should().Be("pending");
        pending.QrBase64.Should().Be("data:image/png;base64,QR");

        var webhook = _factory.Evolution.Webhooks[SeedInstance];
        webhook.GetProperty("url").GetString().Should().Be("http://ocap-api:5000/api/webhooks/whatsapp");
        webhook.GetProperty("headers").GetProperty(EvolutionInstanceIdentity.SignatureHeader).GetString()
            .Should().Be(EvolutionInstanceIdentity.SignatureFor(SeedInstance, WhatsAppEvolutionFactory.WebhookSecret));

        // 2. El teléfono escanea el QR: Evolution avisa por webhook y la conexión queda vinculada.
        _factory.Evolution.Instances[SeedInstance] = "open";
        var connected = await PostWebhookAsync(SeedInstance, $$"""
            { "event": "connection.update", "instance": "{{SeedInstance}}",
              "data": { "instance": "{{SeedInstance}}", "wuid": "573009998877@s.whatsapp.net", "profileName": "Tienda", "state": "open" } }
            """);
        connected.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await admin.GetFromJsonAsync<LinkStatus>("/api/channels/whatsapp/link");
        status!.State.Should().Be("linked");
        status.PhoneNumber.Should().Be("573009998877");

        // 3. Un contacto escribe: el asistente responde por la instancia de la organización, una sola vez.
        var message = MessageUpsert(SeedInstance, "MSG-1", "573001234567", "Hola, ¿abren hoy?");
        (await PostWebhookAsync(SeedInstance, message)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PostWebhookAsync(SeedInstance, message)).StatusCode.Should().Be(HttpStatusCode.OK);

        var replies = _factory.Evolution.SentTexts.Where(sent => sent.Number == "573001234567").ToList();
        replies.Should().ContainSingle();
        replies[0].Instance.Should().Be(SeedInstance);
        replies[0].Text.Should().NotBeNullOrWhiteSpace();

        // 4. Desvincular: cierra la sesión, borra la instancia y la conexión.
        (await admin.DeleteAsync("/api/channels/whatsapp/link")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        _factory.Evolution.Instances.Should().NotContainKey(SeedInstance);
        (await admin.GetFromJsonAsync<LinkStatus>("/api/channels/whatsapp/link"))!.State.Should().Be("unlinked");
    }

    [Fact]
    public async Task GenericConnect_CannotCreateAWhatsAppConnectionPointingToAnotherInstance()
    {
        var admin = await AdminClientAsync();
        var victim = EvolutionInstanceIdentity.InstanceNameFor(Guid.NewGuid());

        var response = await admin.PostAsJsonAsync("/api/channels/connect", new
        {
            provider = "WhatsApp",
            displayName = "WhatsApp",
            credentials = "x",
            metadata = new Dictionary<string, string> { ["ConnectionMode"] = "evolution", ["Instance"] = victim }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rename_IsKeptAfterReconnecting_AndDeleteUnlinksThePhone()
    {
        using var factory = new WhatsAppEvolutionFactory();
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<Session>())!.AccessToken);

        (await admin.PostAsync("/api/channels/whatsapp/link", null)).EnsureSuccessStatusCode();
        var connectionId = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single().Id;

        (await admin.PatchAsJsonAsync($"/api/channels/connections/{connectionId}", new { displayName = " " }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PatchAsJsonAsync($"/api/channels/connections/{connectionId}", new { displayName = "Ventas Panadería" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Evolution avisa que el teléfono se vinculó: el nombre elegido no se pisa con "WhatsApp +número".
        factory.Evolution.Instances[SeedInstance] = "open";
        var client = factory.CreateClient();
        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        {
            Content = new StringContent($$"""
                { "event": "connection.update", "instance": "{{SeedInstance}}",
                  "data": { "instance": "{{SeedInstance}}", "wuid": "573009998877@s.whatsapp.net", "state": "open" } }
                """, Encoding.UTF8, "application/json")
        })
        {
            request.Headers.Add(EvolutionInstanceIdentity.SignatureHeader, EvolutionInstanceIdentity.SignatureFor(SeedInstance, WhatsAppEvolutionFactory.WebhookSecret));
            (await client.SendAsync(request)).EnsureSuccessStatusCode();
        }
        (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single().DisplayName.Should().Be("Ventas Panadería");

        (await admin.DeleteAsync($"/api/channels/{connectionId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Evolution.Instances.Should().NotContainKey(SeedInstance);
        (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Webhook_WithAnInvalidSignature_IsRejected()
    {
        var response = await PostWebhookAsync(
            SeedInstance,
            MessageUpsert(SeedInstance, "MSG-FORGED", "573005550000", "hola"),
            signature: "firma-falsa");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.Evolution.SentTexts.Should().NotContain(sent => sent.Number == "573005550000");
    }

    [Fact]
    public async Task Webhook_SignedForAnotherOrganization_IsRejected()
    {
        var otherInstance = EvolutionInstanceIdentity.InstanceNameFor(Guid.NewGuid());
        var otherSignature = EvolutionInstanceIdentity.SignatureFor(otherInstance, WhatsAppEvolutionFactory.WebhookSecret);

        var response = await PostWebhookAsync(
            SeedInstance,
            MessageUpsert(SeedInstance, "MSG-CROSS", "573005550001", "hola"),
            signature: otherSignature);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_ForAnOrganizationWithoutWhatsApp_DoesNotReply()
    {
        var tenant = Guid.NewGuid();
        var instance = EvolutionInstanceIdentity.InstanceNameFor(tenant);

        var response = await PostWebhookAsync(instance, MessageUpsert(instance, "MSG-NOLINK", "573005550002", "hola"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Evolution.SentTexts.Should().NotContain(sent => sent.Number == "573005550002");
    }

    [Fact]
    public async Task Webhook_UnsignedEventThatIsNotProcessed_IsAcknowledgedWithoutEffects()
    {
        var client = _factory.CreateClient();
        var json = $$"""{ "event": "logout.instance", "instance": "{{SeedInstance}}", "data": { "state": "close" } }""";

        var response = await client.PostAsync("/api/webhooks/whatsapp", new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_UnsignedMessage_IsRejected()
    {
        var client = _factory.CreateClient();
        var json = MessageUpsert(SeedInstance, "MSG-UNSIGNED", "573005550003", "hola");

        var response = await client.PostAsync("/api/webhooks/whatsapp", new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.Evolution.SentTexts.Should().NotContain(sent => sent.Number == "573005550003");
    }

    [Fact]
    public async Task Webhook_FromAnInstanceThatIsNotAnOrganization_IsRejected()
    {
        var response = await PostWebhookAsync("ocap-main", MessageUpsert("ocap-main", "MSG-MAIN", "573005550004", "hola"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_InvalidJson_IsBadRequest()
    {
        var response = await PostWebhookAsync(SeedInstance, "{no es json");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage> PostWebhookAsync(string instance, string json, string? signature = null)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add(
            EvolutionInstanceIdentity.SignatureHeader,
            signature ?? EvolutionInstanceIdentity.SignatureFor(instance, WhatsAppEvolutionFactory.WebhookSecret));
        return await client.SendAsync(request);
    }

    private static string MessageUpsert(string instance, string id, string from, string text) => $$"""
        { "event": "messages.upsert", "instance": "{{instance}}",
          "data": { "key": { "remoteJid": "{{from}}@s.whatsapp.net", "fromMe": false, "id": "{{id}}" },
                    "pushName": "Cliente", "message": { "conversation": "{{text}}" }, "messageType": "conversation",
                    "messageTimestamp": 1761200000 } }
        """;

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    private sealed record Session(string AccessToken);
    private sealed record Connection(Guid Id, string DisplayName);
    private sealed record Envelope(List<Connection> Data);

    private sealed record LinkStatus(string State, string? QrBase64, string? PairingCode, string? PhoneNumber, string? ProfileName);
}

public class WhatsAppWebhookWithoutSecretTests : IClassFixture<WhatsAppWithoutSecretFactory>
{
    private readonly WhatsAppWithoutSecretFactory _factory;

    public WhatsAppWebhookWithoutSecretTests(WhatsAppWithoutSecretFactory factory) => _factory = factory;

    [Fact]
    public async Task Webhook_WithoutAConfiguredSecret_FailsClosed()
    {
        var instance = EvolutionInstanceIdentity.InstanceNameFor(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var json = $$"""{ "event": "messages.upsert", "instance": "{{instance}}", "data": {} }""";

        var response = await _factory.CreateClient().PostAsync(
            "/api/webhooks/whatsapp",
            new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
