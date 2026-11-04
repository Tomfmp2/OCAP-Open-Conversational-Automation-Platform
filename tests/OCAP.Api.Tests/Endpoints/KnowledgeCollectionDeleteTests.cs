using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class KnowledgeCollectionDeleteTests
{
    private static async Task<Guid> CreateCollectionAsync(HttpClient admin, string name)
    {
        var created = await admin.PostAsJsonAsync("/api/knowledge", new { name, description = "" });
        created.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task UnaColeccionLibre_SeEliminaConSusDocumentos()
    {
        using var factory = new GeminiEmbeddingsFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            providerName = "Gemini", displayName = "Gemini", modelName = "gemini-3.5-flash", apiKey = "AIza-no-real",
        })).EnsureSuccessStatusCode();
        var id = await CreateCollectionAsync(admin, "Temporal");
        (await admin.PostAsJsonAsync("/api/knowledge/text", new { knowledgeBaseId = id, title = "Nota", content = "Contenido de prueba" })).EnsureSuccessStatusCode();

        var deleted = await admin.DeleteAsync($"/api/knowledge/base/{id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync());
        (await admin.GetStringAsync("/api/knowledge")).Should().NotContain("Temporal");
    }

    [Fact]
    public async Task UnaColeccionEnUsoPorUnCanal_NoSeElimina()
    {
        // Sin esta protección, el canal quedaría con la lista vacía, que significa «todas las colecciones».
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var id = await CreateCollectionAsync(admin, "Precios");
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var channel = JsonDocument.Parse(await admin.GetStringAsync("/api/channels/connections")).RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();
        (await admin.PutAsJsonAsync($"/api/channels/connections/{channel}/capabilities", new { knowledgeBaseIds = new[] { id }, tools = Array.Empty<string>() })).EnsureSuccessStatusCode();

        var deleted = await admin.DeleteAsync($"/api/knowledge/base/{id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await deleted.Content.ReadAsStringAsync()).Should().Contain("Sitio");
        (await admin.GetStringAsync("/api/knowledge")).Should().Contain("Precios");
    }

    [Fact]
    public async Task UnaColeccionInexistente_Da404()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);

        (await admin.DeleteAsync($"/api/knowledge/base/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
