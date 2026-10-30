using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Services;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class ChannelShutdownTests
{
    [Fact]
    public async Task ShuttingDown_KeepsOngoingChats_RejectsNewOnes_AndTurnsOffWhenTheyGoQuiet()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var channel = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Provider == "WebChat");
        var key = channel.ConfigurationMetadata["PublicKey"];
        var visitor = factory.CreateClient();
        Task<SendResult?> Send(Guid session, string text) =>
            visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = text })
                .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<SendResult>()).Unwrap();

        var ongoing = Guid.NewGuid();
        (await Send(ongoing, "Hola"))!.Reply.Should().NotBeNullOrEmpty();

        (await admin.PostAsync($"/api/channels/{channel.Id}/shutdown", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Send(ongoing, "¿Y el precio?"))!.Reply.Should().NotBeNullOrEmpty("el chat estaba en curso");
        (await Send(Guid.NewGuid(), "Hola, soy nuevo"))!.Reply.Should().BeNull("el canal ya no acepta chats nuevos");

        var job = factory.Services.GetRequiredService<ChannelShutdownBackgroundService>();
        await job.CompleteShutdownsAsync(DateTime.UtcNow, CancellationToken.None);
        (await FindAsync(admin, channel.Id)).Enabled.Should().BeTrue("todavía hay un chat en curso");

        await job.CompleteShutdownsAsync(DateTime.UtcNow.AddMinutes(6), CancellationToken.None);
        var off = await FindAsync(admin, channel.Id);
        off.Enabled.Should().BeFalse();
        off.ConfigurationMetadata.Should().NotContainKey(ChannelShutdown.ShuttingDownSinceKey);
        (await Send(ongoing, "¿Siguen ahí?"))!.Reply.Should().BeNull("apagado no responde a nadie");

        (await admin.PostAsync($"/api/channels/{channel.Id}/enable", null)).EnsureSuccessStatusCode();
        (await Send(Guid.NewGuid(), "Hola de nuevo"))!.Reply.Should().NotBeNullOrEmpty();
    }

    private static async Task<Connection> FindAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Id == id);

    private sealed record SendResult(string? Reply);
    private sealed record Connection(Guid Id, string Provider, bool Enabled, Dictionary<string, string> ConfigurationMetadata);
    private sealed record Envelope(List<Connection> Data);
}
