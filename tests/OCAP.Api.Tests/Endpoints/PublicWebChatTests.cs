using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class PublicWebChatTests
{
    [Fact]
    public async Task AVisitor_ChatsWithTheChannelAgent_AndReceivesTheTeamsReplies()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var (connectionId, key) = await CreateWidgetAsync(admin);
        var visitor = factory.CreateClient();
        var session = Guid.NewGuid();

        (await visitor.GetAsync($"/api/public/webchat/{key}/config")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await visitor.GetAsync("/api/public/webchat/clave-inventada/config")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var sent = await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = "¿Hacen envíos?" });
        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        (await sent.Content.ReadFromJsonAsync<SendResult>())!.Reply.Should().NotBeNullOrWhiteSpace();

        // La conversación queda en la bandeja; una persona la toma y responde desde el panel.
        var inbox = await admin.GetFromJsonAsync<List<InboxItem>>("/api/inbox");
        var conversation = inbox!.Single(c => c.Channel == "WebChat");
        conversation.CanReply.Should().BeTrue();
        (await admin.PostAsync($"/api/inbox/{conversation.Id}/takeover", null)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/inbox/{conversation.Id}/reply", new { text = "Sí, a todo el país." })).EnsureSuccessStatusCode();

        var messages = (await visitor.GetFromJsonAsync<List<WidgetMessage>>($"/api/public/webchat/{key}/sessions/{session}/messages"))!;
        messages.Select(m => m.From).Should().ContainInOrder("visitor", "agent", "operator");
        messages.Last().Text.Should().Be("Sí, a todo el país.");

        // Otra sesión no ve esa conversación.
        (await visitor.GetFromJsonAsync<List<WidgetMessage>>($"/api/public/webchat/{key}/sessions/{Guid.NewGuid()}/messages"))
            .Should().BeEmpty();
        _ = connectionId;
    }

    [Fact]
    public async Task WithAllowedDomains_OnlyThoseSitesCanUseTheWidget()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var (connectionId, key) = await CreateWidgetAsync(admin);

        (await admin.PutAsJsonAsync($"/api/channels/webchat/{connectionId}/widget", new { allowedOrigins = new[] { "https://tienda.com/carrito" } }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync($"/api/channels/webchat/{connectionId}/widget", new { allowedOrigins = new[] { "https://tienda.com" } }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var visitor = factory.CreateClient();
        (await ConfigFrom(visitor, key, "https://otro-sitio.com")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ConfigFrom(visitor, key, null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var allowed = await ConfigFrom(visitor, key, "https://tienda.com");
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        allowed.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }

    [Fact]
    public async Task RotatingTheKey_DisablesTheOldOne()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var (connectionId, key) = await CreateWidgetAsync(admin);

        var rotated = await admin.PostAsync($"/api/channels/webchat/{connectionId}/widget/key", null);
        var newKey = (await rotated.Content.ReadFromJsonAsync<KeyResponse>())!.PublicKey;

        var visitor = factory.CreateClient();
        (await visitor.GetAsync($"/api/public/webchat/{key}/config")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await visitor.GetAsync($"/api/public/webchat/{newKey}/config")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<(Guid Id, string Key)> CreateWidgetAsync(HttpClient admin)
    {
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var connection = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Provider == "WebChat");
        return (connection.Id, connection.ConfigurationMetadata["PublicKey"]);
    }

    private static Task<HttpResponseMessage> ConfigFrom(HttpClient client, string key, string? origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/public/webchat/{key}/config");
        if (origin is not null) request.Headers.Add("Origin", origin);
        return client.SendAsync(request);
    }

    private sealed record SendResult(string? Reply);
    private sealed record KeyResponse(string PublicKey);
    private sealed record WidgetMessage(string From, string Text);
    private sealed record InboxItem(Guid Id, string Channel, bool CanReply);
    private sealed record Connection(Guid Id, string Provider, Dictionary<string, string> ConfigurationMetadata);
    private sealed record Envelope(List<Connection> Data);
}
