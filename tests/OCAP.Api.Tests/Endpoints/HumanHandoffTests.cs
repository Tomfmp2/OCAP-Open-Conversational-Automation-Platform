using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Tests.Endpoints;

public class HumanHandoffTests
{
    [Fact]
    public async Task TheAgentStaysQuietWhileSomeoneAttends_AndResumesAfterFifteenMinutesWithoutTheTeam()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var key = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Provider == "WebChat").ConfigurationMetadata["PublicKey"];
        var visitor = factory.CreateClient();
        var session = Guid.NewGuid();
        async Task<string?> Send(string text) =>
            (await (await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = text }))
                .Content.ReadFromJsonAsync<SendResult>())!.Reply;

        (await Send("Hola")).Should().NotBeNullOrEmpty();
        var conversation = (await admin.GetFromJsonAsync<List<InboxItem>>("/api/inbox"))!.Single(c => c.Channel == "WebChat");
        (await admin.PostAsJsonAsync($"/api/inbox/{conversation.Id}/reply", new { text = "Te atiendo yo." })).EnsureSuccessStatusCode();

        (await Send("Gracias")).Should().BeNull("una persona atiende la conversación");

        // Pasan 16 minutos sin que el equipo escriba.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            foreach (var message in await db.Messages.IgnoreQueryFilters().Where(m => m.ConversationId == conversation.Id).ToListAsync())
                db.Entry(message).Property(m => m.CreatedAt).CurrentValue = message.CreatedAt.AddMinutes(-16);
            await db.SaveChangesAsync();
        }

        (await Send("¿Siguen ahí?")).Should().NotBeNullOrEmpty("el agente retoma la conversación");
    }

    private sealed record SendResult(string? Reply);
    private sealed record InboxItem(Guid Id, string Channel);
    private sealed record Connection(Guid Id, string Provider, Dictionary<string, string> ConfigurationMetadata);
    private sealed record Envelope(List<Connection> Data);
}
