using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class ChannelCapabilitiesTests
{
    [Fact]
    public async Task AChannel_ChoosesItsKnowledgeAndActions_FromWhatTheOrganizationHas()
    {
        using var factory = new OcapApiFactory();
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var kb = await admin.PostAsJsonAsync("/api/knowledge", new { name = "Precios", description = "" });
        var baseId = JsonDocument.Parse(await kb.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var channelId = JsonDocument.Parse(await admin.GetStringAsync("/api/channels/connections")).RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();
        var url = $"/api/channels/connections/{channelId}/capabilities";

        (await admin.PutAsJsonAsync(url, new { knowledgeBaseIds = new[] { Guid.NewGuid() }, tools = Array.Empty<string>() }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "una colección de otra organización no se puede asignar");
        (await admin.PutAsJsonAsync(url, new { knowledgeBaseIds = Array.Empty<Guid>(), tools = new[] { "SendEmailTool" } }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "el correo del negocio no se concede a clientes");

        (await admin.PutAsJsonAsync(url, new { knowledgeBaseIds = new[] { baseId }, tools = new[] { "AppendSpreadsheetRowTool" } }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var metadata = JsonDocument.Parse(await admin.GetStringAsync("/api/channels/connections")).RootElement.GetProperty("data")[0].GetProperty("configurationMetadata");
        metadata.GetProperty("KnowledgeBaseIds").GetString().Should().Be(baseId.ToString());
        metadata.GetProperty("Tools").GetString().Should().Be("AppendSpreadsheetRowTool");
    }
}
