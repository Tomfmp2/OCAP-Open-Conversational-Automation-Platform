using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Channels.WhatsApp.Services;

namespace OCAP.Api.Tests.Endpoints;

public sealed class WhatsAppCloudFactory : OcapApiFactory
{
    public const string AppSecret = "meta-app-secret-de-pruebas";
    public FakeMetaGraph Graph { get; } = new();

    protected override IDictionary<string, string?> ExtraSettings => new Dictionary<string, string?>
    {
        ["WhatsApp:AppSecret"] = AppSecret
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.AddHttpClient<WhatsAppApiClient>().ConfigurePrimaryHttpMessageHandler(() => Graph));
    }
}

/// <summary>Graph API de Meta en memoria: valida cualquier número y registra los mensajes enviados.</summary>
public sealed class FakeMetaGraph : HttpMessageHandler
{
    public ConcurrentQueue<(string PhoneNumberId, string To, string Body, string? Token)> Sent { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
        if (request.Method == HttpMethod.Post && segments is [_, var phoneId, "messages"])
        {
            var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement;
            Sent.Enqueue((phoneId, json.GetProperty("to").GetString()!, json.GetProperty("text").GetProperty("body").GetString()!,
                request.Headers.Authorization?.Parameter));
        }
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"ok\"}", Encoding.UTF8, "application/json") };
    }
}

public class WhatsAppCloudWebhookTests : IClassFixture<WhatsAppCloudFactory>
{
    private readonly WhatsAppCloudFactory _factory;

    public WhatsAppCloudWebhookTests(WhatsAppCloudFactory factory) => _factory = factory;

    [Fact]
    public async Task SignedMessage_ToAConnectedNumber_IsAnsweredOnce()
    {
        var admin = await AdminClientAsync();
        var connected = await admin.PostAsJsonAsync("/api/channels/whatsapp/connect",
            new { displayName = "Cloud", phoneNumberId = "1099", apiToken = "meta-token" });
        connected.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = Payload("1099", "wamid.1", "573001112233", "Hola desde Cloud");
        (await PostSignedAsync(payload)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PostSignedAsync(payload)).StatusCode.Should().Be(HttpStatusCode.OK);

        var replies = _factory.Graph.Sent.Where(s => s.To == "573001112233").ToList();
        replies.Should().ContainSingle();
        replies[0].PhoneNumberId.Should().Be("1099");
        replies[0].Token.Should().Be("meta-token");

        // El mismo número no puede conectarse otra vez.
        (await admin.PostAsJsonAsync("/api/channels/whatsapp/connect",
            new { displayName = "Otro", phoneNumberId = "1099", apiToken = "meta-token" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnsignedOrUnknownNumber_IsNotAnswered()
    {
        var unsigned = new StringContent(Payload("1099", "wamid.2", "573004445566", "hola"), Encoding.UTF8, "application/json");
        (await _factory.CreateClient().PostAsync("/api/channels/whatsapp/webhook", unsigned)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await PostSignedAsync(Payload("5555", "wamid.3", "573007778899", "hola"))).StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Graph.Sent.Should().NotContain(s => s.To == "573004445566" || s.To == "573007778899");
    }

    private static string Payload(string phoneNumberId, string messageId, string from, string text) => JsonSerializer.Serialize(new
    {
        @object = "whatsapp_business_account",
        entry = new[]
        {
            new
            {
                id = "WABA",
                changes = new[]
                {
                    new
                    {
                        field = "messages",
                        value = new
                        {
                            messaging_product = "whatsapp",
                            metadata = new { display_phone_number = "15550001099", phone_number_id = phoneNumberId },
                            contacts = new[] { new { profile = new { name = "Ana" }, wa_id = from } },
                            messages = new[] { new { from, id = messageId, timestamp = "1761200000", type = "text", text = new { body = text } } }
                        }
                    }
                }
            }
        }
    });

    private async Task<HttpResponseMessage> PostSignedAsync(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WhatsAppCloudFactory.AppSecret));
        var signature = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/channels/whatsapp/webhook")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", $"sha256={signature}");
        return await _factory.CreateClient().SendAsync(request);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    private sealed record Session(string AccessToken);
}
