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

    private sealed record Reply(Guid ConversationId);
    private sealed record Envelope(Reply Data);
    private sealed record Summary(Guid Id, string Title);
    private sealed record Line(string Role, string Content);
}
