using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

/// <summary>Un canal con agente y modelo de IA propios responde y guarda la conversación (bajo RLS en Postgres).</summary>
public class ChannelWithAssignedAiTests
{
    [Fact]
    public async Task AChannelWithItsOwnAgentAndModel_AnswersVisitors()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var ai = await admin.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            providerName = "Gemini", displayName = "Gemini canal", modelName = "gemini-3.5-flash", apiKey = "AIza-no-real"
        });
        ai.EnsureSuccessStatusCode();
        var aiId = JsonDocument.Parse(await ai.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        var agent = await admin.PostAsJsonAsync("/api/agents", new { name = "Cocina", description = "", systemPrompt = "Recetas.", allowedTools = Array.Empty<string>() });
        var agentId = JsonDocument.Parse(await agent.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var channel = JsonDocument.Parse(await admin.GetStringAsync("/api/channels/connections")).RootElement.GetProperty("data")[0];
        var channelId = channel.GetProperty("id").GetGuid();
        var key = channel.GetProperty("configurationMetadata").GetProperty("PublicKey").GetString();
        (await admin.PutAsJsonAsync($"/api/channels/connections/{channelId}/agent", new { agentId })).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync($"/api/channels/connections/{channelId}/ai", new { aiProviderConfigId = aiId })).EnsureSuccessStatusCode();

        var visitor = factory.CreateClient();
        var sent = await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = Guid.NewGuid(), message = "Hola" });

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WithIndexedKnowledge_TheChannelStillAnswers()
    {
        // Regresión: la búsqueda vectorial abría la conexión sin el interceptor de RLS y el resto de la petición quedaba
        // sin organización (no veía el modelo de IA y no podía guardar la respuesta).
        using var factory = new GeminiEmbeddingsFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        (await admin.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            providerName = "Gemini", displayName = "Gemini", modelName = "gemini-3.5-flash", apiKey = "AIza-no-real"
        })).EnsureSuccessStatusCode();
        var kb = await admin.PostAsJsonAsync("/api/knowledge", new { name = "Recetas", description = "" });
        var kbId = JsonDocument.Parse(await kb.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        (await admin.PostAsJsonAsync("/api/knowledge/text", new { knowledgeBaseId = kbId, title = "Empanadas", content = "Masa de maíz y carne." }))
            .EnsureSuccessStatusCode();

        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var key = JsonDocument.Parse(await admin.GetStringAsync("/api/channels/connections")).RootElement.GetProperty("data")[0]
            .GetProperty("configurationMetadata").GetProperty("PublicKey").GetString();
        var visitor = factory.CreateClient();
        var session = Guid.NewGuid();

        var sent = await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = session, message = "¿Cómo hago empanadas?" });

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        var messages = JsonDocument.Parse(await visitor.GetStringAsync($"/api/public/webchat/{key}/sessions/{session}/messages")).RootElement;
        messages.GetArrayLength().Should().Be(2, "se guardan el mensaje y la respuesta");
    }
}
