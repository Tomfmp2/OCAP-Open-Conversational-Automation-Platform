using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

/// <summary>Agente asignado a un bot de Telegram y bandeja: tomar, responder como persona y devolver al agente.</summary>
public class AgentsAndInboxTests : TelegramBotTestsBase
{
    public AgentsAndInboxTests(TelegramFactory factory) : base(factory) { }

    [Fact]
    public async Task Agent_Assignment_Handoff_AndOperatorReply()
    {
        var admin = await AdminClientAsync();

        // 1. Crear un agente (con su prompt) y probarlo desde el panel sin canales.
        var created = await admin.PostAsJsonAsync("/api/agents", new
        {
            name = "Ventas Sol",
            description = "Atiende pedidos",
            systemPrompt = "Eres el vendedor de la Panadería Sol.",
            allowedTools = Array.Empty<string>()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var agent = (await created.Content.ReadFromJsonAsync<AgentDto>())!;
        agent.SystemPrompt.Should().Be("Eres el vendedor de la Panadería Sol.");

        (await admin.PostAsJsonAsync("/api/agents", new { name = "ventas sol", description = "", systemPrompt = "x", allowedTools = Array.Empty<string>() }))
            .IsSuccessStatusCode.Should().BeFalse("el nombre es único por organización");

        var test = await admin.PostAsJsonAsync($"/api/agents/{agent.Id}/test", new { message = "Hola", history = Array.Empty<object>() });
        test.StatusCode.Should().Be(HttpStatusCode.OK);
        (await test.Content.ReadFromJsonAsync<AgentTest>())!.AgentName.Should().Be("Ventas Sol");
        (await admin.PostAsJsonAsync($"/api/agents/{Guid.NewGuid()}/test", new { message = "Hola" })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. Conectar un bot y asignarle el agente.
        const string token = "333:inbox-bot-token";
        var bot = await admin.PostAsJsonAsync("/api/channels/telegram/bots", new
        {
            displayName = "Pedidos", botToken = token, connectionMode = "webhook", webhookBaseUrl = "https://ocap.example.com"
        });
        bot.StatusCode.Should().Be(HttpStatusCode.Created);
        var (url, secret) = _factory.Telegram.Webhooks[token];
        var connectionId = Guid.Parse(url[(url.LastIndexOf('/') + 1)..]);

        (await admin.PutAsJsonAsync($"/api/channels/connections/{connectionId}/agent", new { agentId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync($"/api/channels/connections/{connectionId}/agent", new { agentId = agent.Id }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. Un cliente escribe: responde el agente y la conversación aparece en la bandeja.
        var path = new Uri(url).AbsolutePath;
        (await PostUpdateAsync(path, FakeTelegramApi.PrivateMessageUpdate(9001, 9901, "Hola, ¿tienen pan integral?"), secret)).EnsureSuccessStatusCode();
        _factory.Telegram.Sent.Count(s => s.ChatId == "9901").Should().Be(1);

        var inbox = await admin.GetFromJsonAsync<List<InboxItem>>("/api/inbox");
        var item = inbox!.Single(c => c.ExternalContactId == "9901");
        item.Channel.Should().Be("Telegram");
        item.ContactName.Should().Be("Ana");
        item.CanReply.Should().BeTrue();
        item.LastSender.Should().Be("Agent");

        // 4. Tomar la conversación: el agente deja de responder.
        (await admin.PostAsync($"/api/inbox/{item.Id}/takeover", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await PostUpdateAsync(path, FakeTelegramApi.PrivateMessageUpdate(9002, 9901, "¿Hola?"), secret)).EnsureSuccessStatusCode();
        _factory.Telegram.Sent.Count(s => s.ChatId == "9901").Should().Be(1);
        (await admin.GetFromJsonAsync<List<InboxItem>>("/api/inbox?onlyHuman=true"))!.Should().Contain(c => c.Id == item.Id);

        // 5. Responder como persona: sale por el bot del contacto.
        (await admin.PostAsJsonAsync($"/api/inbox/{item.Id}/reply", new { text = "Sí, recién horneado." })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        _factory.Telegram.Sent.Should().Contain(s => s.ChatId == "9901" && s.Token == token && s.Text == "Sí, recién horneado.");

        var messages = await admin.GetFromJsonAsync<List<InboxMessage>>($"/api/inbox/{item.Id}/messages");
        messages!.Select(m => m.Sender).Should().Equal("User", "Agent", "User", "Operator");

        // 6. Devolver al agente: vuelve a responder.
        (await admin.PostAsync($"/api/inbox/{item.Id}/release", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await PostUpdateAsync(path, FakeTelegramApi.PrivateMessageUpdate(9003, 9901, "Gracias"), secret)).EnsureSuccessStatusCode();
        _factory.Telegram.Sent.Count(s => s.ChatId == "9901").Should().Be(3);
    }

    [Fact]
    public async Task Inbox_UnknownConversation_IsNotFound()
    {
        var admin = await AdminClientAsync();
        var id = Guid.NewGuid();

        (await admin.GetAsync($"/api/inbox/{id}/messages")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PostAsync($"/api/inbox/{id}/takeover", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PostAsJsonAsync($"/api/inbox/{id}/reply", new { text = "hola" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PostAsJsonAsync($"/api/inbox/{id}/reply", new { text = " " })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record AgentDto(Guid Id, string Name, string SystemPrompt);
    private sealed record AgentTest(string Reply, string AgentName, bool TransferToHuman);
    private sealed record InboxItem(Guid Id, string Channel, string ContactName, string? ExternalContactId, bool HandledByHuman, bool CanReply, string? LastSender);
    private sealed record InboxMessage(string Sender, string Text);
}
