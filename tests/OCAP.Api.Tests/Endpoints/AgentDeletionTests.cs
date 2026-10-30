using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class AgentDeletionTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public AgentDeletionTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AnAgentAssignedToAChannel_CannotBeDeleted_UntilItIsUnassigned()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        var created = await admin.PostAsJsonAsync("/api/agents", new { name = "Ventas", description = "", systemPrompt = "Vende.", allowedTools = Array.Empty<string>() });
        var agent = (await created.Content.ReadFromJsonAsync<Item>())!;
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Web tienda", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var channel = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.DisplayName == "Web tienda");
        (await admin.PutAsJsonAsync($"/api/channels/connections/{channel.Id}/agent", new { agentId = agent.Id })).EnsureSuccessStatusCode();

        var blocked = await admin.DeleteAsync($"/api/agents/{agent.Id}");
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await blocked.Content.ReadAsStringAsync()).Should().Contain("Web tienda");

        (await admin.PutAsJsonAsync($"/api/channels/connections/{channel.Id}/agent", new { agentId = (Guid?)null })).EnsureSuccessStatusCode();
        (await admin.DeleteAsync($"/api/agents/{agent.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record Item(Guid Id);
    private sealed record Connection(Guid Id, string DisplayName);
    private sealed record Envelope(List<Connection> Data);
}
