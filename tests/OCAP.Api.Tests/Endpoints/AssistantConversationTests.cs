using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class AssistantConversationTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public AssistantConversationTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Conversations_ArePersisted_AndOnlyTheirOwnerCanReadRenameOrDeleteThem()
    {
        var owner = TestAuthHelper.CreateAuthenticatedClient(_factory, TestAuthHelper.CreateAccessToken(_factory, "owner@ocap.test"));
        var colleague = TestAuthHelper.CreateAuthenticatedClient(_factory, TestAuthHelper.CreateAccessToken(_factory, "colleague@ocap.test"));

        var first = await owner.PostAsJsonAsync("/api/agents/principal/messages", new { message = "Hola, ¿qué canales tengo?" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversationId = (await first.Content.ReadFromJsonAsync<Envelope>())!.Data.ConversationId;
        (await owner.PostAsJsonAsync("/api/agents/principal/messages", new { conversationId, message = "¿Y cuántos agentes?" })).EnsureSuccessStatusCode();

        var list = await owner.GetFromJsonAsync<List<Summary>>("/api/agents/principal/conversations");
        list.Should().ContainSingle(c => c.Id == conversationId && c.Title == "Hola, ¿qué canales tengo?");
        var messages = await owner.GetFromJsonAsync<List<Line>>($"/api/agents/principal/conversations/{conversationId}/messages");
        messages!.Select(m => m.Role).Should().Equal("user", "assistant", "user", "assistant");

        (await colleague.GetFromJsonAsync<List<Summary>>("/api/agents/principal/conversations")).Should().BeEmpty();
        (await colleague.GetAsync($"/api/agents/principal/conversations/{conversationId}/messages")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await colleague.PostAsJsonAsync("/api/agents/principal/messages", new { conversationId, message = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await colleague.DeleteAsync($"/api/agents/principal/conversations/{conversationId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await owner.PatchAsJsonAsync($"/api/agents/principal/conversations/{conversationId}", new { title = "Canales" })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await owner.GetFromJsonAsync<List<Summary>>("/api/agents/principal/conversations"))!.Single().Title.Should().Be("Canales");

        (await owner.DeleteAsync($"/api/agents/principal/conversations/{conversationId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await owner.GetFromJsonAsync<List<Summary>>("/api/agents/principal/conversations")).Should().BeEmpty();
    }

    [Fact]
    public async Task Attachments_ReachTheAssistant_AndOnlyTheirNamesAreKeptInTheHistory()
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory, TestAuthHelper.CreateAccessToken(_factory, "files@ocap.test"));
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

        using var form = new MultipartFormDataContent
        {
            { new StringContent("¿Qué ves?"), "message" },
            { new ByteArrayContent(png), "files", "foto.png" },
            { new ByteArrayContent("Horario: 9 a 18"u8.ToArray()), "files", "horario.txt" }
        };
        var response = await client.PostAsync("/api/agents/principal/messages/attachments", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversationId = (await response.Content.ReadFromJsonAsync<Envelope>())!.Data.ConversationId;
        var messages = await client.GetFromJsonAsync<List<Line>>($"/api/agents/principal/conversations/{conversationId}/messages");
        messages![0].Content.Should().Be("¿Qué ves?\n\n[Adjuntos: foto.png, horario.txt]");
    }

    [Fact]
    public async Task Attachments_AreCheckedByContent_AndLimited()
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory, TestAuthHelper.CreateAccessToken(_factory, "files2@ocap.test"));

        using var disguised = new MultipartFormDataContent { { new ByteArrayContent([0x4D, 0x5A, 0x90, 0x00, 0x03]), "files", "factura.pdf" } };
        (await client.PostAsync("/api/agents/principal/messages/attachments", disguised)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var tooMany = new MultipartFormDataContent();
        for (var i = 0; i < 5; i++) tooMany.Add(new ByteArrayContent("hola"u8.ToArray()), "files", $"nota{i}.txt");
        (await client.PostAsync("/api/agents/principal/messages/attachments", tooMany)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record Reply(Guid ConversationId);
    private sealed record Envelope(Reply Data);
    private sealed record Summary(Guid Id, string Title);
    private sealed record Line(string Role, string Content);
}
