using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Services.Memory;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Infrastructure.BackgroundJobs;
using OCAP.Infrastructure.Options;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Tests.Endpoints;

/// <summary>Modelo de memoria simulado: devuelve lo que el test le pida.</summary>
public sealed class FakeMemoryModel : IMemoryModel
{
    public MemoryExtraction? Next { get; set; }
    public int Extractions { get; private set; }

    public Task<string?> SummarizeAsync(Guid tenantId, string? previousSummary, IReadOnlyList<MemoryTurn> turns, CancellationToken cancellationToken) =>
        Task.FromResult<string?>($"Resumen de {turns.Count} mensajes");

    public Task<MemoryExtraction?> ExtractAsync(Guid tenantId, IReadOnlyList<string> knownFacts, IReadOnlyList<MemoryTurn> turns, CancellationToken cancellationToken)
    {
        Extractions++;
        return Task.FromResult(Next);
    }
}

public sealed class MemoryFactory : OcapApiFactory
{
    public FakeMemoryModel Model { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddScoped<IMemoryModel>(_ => Model));
    }
}

public class ContactMemoryTests
{
    [Fact]
    public async Task AContact_IsRemembered_Updated_Forgotten_AndExpires()
    {
        using var factory = new MemoryFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var key = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Provider == "WebChat").ConfigurationMetadata["PublicKey"];
        var visitor = factory.CreateClient();
        var session = Guid.NewGuid();
        async Task<string?> Send(string text) =>
            (await (await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = text }))
                .Content.ReadFromJsonAsync<SendResult>())!.Reply;
        var job = factory.Services.GetRequiredService<ConversationMemoryBackgroundService>();

        await Send("Hola, soy Ana de Medellín");
        await Send("Quiero dos docenas de empanadas");
        var conversation = (await admin.GetFromJsonAsync<List<InboxItem>>("/api/inbox"))!.Single(c => c.Channel == "WebChat");

        // Mientras la conversación sigue activa no se extrae nada.
        factory.Model.Next = new MemoryExtraction("Pidió dos docenas de empanadas.", [new ExtractedFact("ciudad", "Vive en Medellín")]);
        await job.RunOnceAsync(DateTime.UtcNow, CancellationToken.None);
        factory.Model.Extractions.Should().Be(0);

        await job.RunOnceAsync(DateTime.UtcNow.AddMinutes(11), CancellationToken.None);
        var memory = await admin.GetFromJsonAsync<List<MemoryItem>>($"/api/inbox/{conversation.Id}/memory");
        memory!.Select(m => m.Text).Should().BeEquivalentTo("Vive en Medellín", "Pidió dos docenas de empanadas.");

        // Un dato nuevo con la misma clave reemplaza al anterior.
        await Send("Me mudé a Cali");
        await Send("¿Envían allá?");
        factory.Model.Next = new MemoryExtraction(null, [new ExtractedFact("ciudad", "Vive en Cali")]);
        await job.RunOnceAsync(DateTime.UtcNow.AddMinutes(11), CancellationToken.None);
        memory = await admin.GetFromJsonAsync<List<MemoryItem>>($"/api/inbox/{conversation.Id}/memory");
        memory!.Where(m => m.Kind == "Fact").Select(m => m.Text).Should().Equal("Vive en Cali");

        // «Olvídame» lo borra todo sin pasar por el modelo.
        (await Send("olvídame")).Should().Contain("borré lo que recordaba");
        (await admin.GetFromJsonAsync<List<MemoryItem>>($"/api/inbox/{conversation.Id}/memory")).Should().BeEmpty();

        // Lo que se vuelve a guardar caduca con el plazo de la organización.
        await Send("Soy Ana otra vez");
        await Send("¿Tienen arepas?");
        (await admin.PutAsJsonAsync("/api/settings/preferences", new { memoryDays = 30 })).EnsureSuccessStatusCode();
        factory.Model.Next = new MemoryExtraction(null, [new ExtractedFact("nombre", "Se llama Ana")]);
        await job.RunOnceAsync(DateTime.UtcNow.AddMinutes(11), CancellationToken.None);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            var saved = await db.ContactMemories.IgnoreQueryFilters().SingleAsync();
            saved.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(11).AddDays(30), TimeSpan.FromMinutes(1));
            db.Entry(saved).Property(m => m.ExpiresAt).CurrentValue = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        var retention = factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<AuditAndOutboxRetentionBackgroundService>().Single();
        await retention.PerformRetentionPurgeAsync(new RetentionOptions(), CancellationToken.None);
        (await admin.GetFromJsonAsync<List<MemoryItem>>($"/api/inbox/{conversation.Id}/memory")).Should().BeEmpty();
    }

    [Fact]
    public async Task WithMemoryOff_NothingIsExtracted()
    {
        using var factory = new MemoryFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PutAsJsonAsync("/api/settings/preferences", new { memoryDays = 0 })).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var key = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Provider == "WebChat").ConfigurationMetadata["PublicKey"];
        var visitor = factory.CreateClient();
        var session = Guid.NewGuid();
        foreach (var text in new[] { "Hola, soy Ana", "Vivo en Bogotá" })
            (await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = text })).StatusCode.Should().Be(HttpStatusCode.OK);

        factory.Model.Next = new MemoryExtraction("x", [new ExtractedFact("ciudad", "Bogotá")]);
        await factory.Services.GetRequiredService<ConversationMemoryBackgroundService>().RunOnceAsync(DateTime.UtcNow.AddMinutes(11), CancellationToken.None);

        factory.Model.Extractions.Should().Be(0);
    }

    [Fact]
    public async Task ALongConversation_IsSummarized_AndTheSummaryIsKept()
    {
        using var factory = new MemoryFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var key = (await admin.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.Single(c => c.Provider == "WebChat").ConfigurationMetadata["PublicKey"];
        var visitor = factory.CreateClient();
        var session = Guid.NewGuid();
        for (var i = 0; i < 10; i++) // 10 mensajes y 10 respuestas: 20, más que la ventana de 12
            await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = $"Mensaje {i}" });

        await factory.Services.GetRequiredService<ConversationMemoryBackgroundService>().RunOnceAsync(DateTime.UtcNow, CancellationToken.None);

        using var scope = factory.Services.CreateScope();
        var conversation = await scope.ServiceProvider.GetRequiredService<OCAPDbContext>().Conversations.IgnoreQueryFilters().SingleAsync();
        conversation.Summary.Should().Be("Resumen de 8 mensajes");
        conversation.SummarizedUntil.Should().NotBeNull();
    }

    private sealed record SendResult(string? Reply);
    private sealed record InboxItem(Guid Id, string Channel);
    private sealed record MemoryItem(Guid Id, string Kind, string? Key, string Text);
    private sealed record Connection(Guid Id, string Provider, Dictionary<string, string> ConfigurationMetadata);
    private sealed record Envelope(List<Connection> Data);
}
