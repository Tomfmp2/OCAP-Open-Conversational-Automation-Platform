using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class KnowledgeAuthoringTests : IClassFixture<GeminiEmbeddingsFactory>
{
    private readonly GeminiEmbeddingsFactory _factory;

    public KnowledgeAuthoringTests(GeminiEmbeddingsFactory factory) => _factory = factory;

    [Fact]
    public async Task AWrittenAnswer_BecomesADocument_WhoseContentCanBeRead()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        // Indexar necesita embeddings: Gemini simulado con una clave de la organización.
        (await admin.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            providerName = "Gemini", displayName = "Gemini", modelName = "gemini-3.5-flash", apiKey = "AIza-prueba"
        })).EnsureSuccessStatusCode();
        var baseId = await CreateBaseAsync(admin, "Preguntas frecuentes");

        var added = await admin.PostAsJsonAsync("/api/knowledge/text", new
        {
            knowledgeBaseId = baseId, title = "¿Abren los sábados?", content = "Sí, de 9 a 14."
        });
        added.StatusCode.Should().Be(HttpStatusCode.OK, await added.Content.ReadAsStringAsync());
        var documentId = JsonDocument.Parse(await added.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var content = await WaitForContentAsync(admin, documentId);
        string.Join("\n", content).Should().Contain("Sí, de 9 a 14.");

        (await admin.PostAsJsonAsync("/api/knowledge/text", new { knowledgeBaseId = baseId, title = "", content = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportingAUrl_OnlyReachesTheInternet()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        var baseId = await CreateBaseAsync(admin, "Web");

        (await admin.PostAsJsonAsync("/api/knowledge/url", new { knowledgeBaseId = baseId, url = "http://169.254.169.254/latest/meta-data" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PostAsJsonAsync("/api/knowledge/url", new { knowledgeBaseId = baseId, url = "no es una url" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnAgent_KeepsTheCollectionsItUses()
    {
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        var baseId = await CreateBaseAsync(admin, "Precios");

        var created = await admin.PostAsJsonAsync("/api/agents", new
        {
            name = "Ventas precios", description = "", systemPrompt = "Vende.", allowedTools = Array.Empty<string>(), knowledgeBaseIds = new[] { baseId }
        });
        var agent = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement;
        agent.GetProperty("knowledgeBaseIds").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(baseId);
    }

    private static async Task<Guid> CreateBaseAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/knowledge", new { name, description = "" });
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<List<string>> WaitForContentAsync(HttpClient client, Guid documentId)
    {
        // El documento se procesa en segundo plano.
        for (var i = 0; i < 40; i++)
        {
            var body = JsonDocument.Parse(await client.GetStringAsync($"/api/knowledge/documents/{documentId}/content")).RootElement;
            var chunks = body.GetProperty("chunks").EnumerateArray().Select(c => c.GetString()!).ToList();
            if (chunks.Count > 0) return chunks;
            await Task.Delay(250);
        }
        return [];
    }
}
