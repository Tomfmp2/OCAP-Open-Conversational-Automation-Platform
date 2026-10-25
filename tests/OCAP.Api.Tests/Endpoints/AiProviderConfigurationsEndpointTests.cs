using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class AiProviderConfigurationsEndpointTests : IClassFixture<OcapApiFactory>
{
    private static readonly Guid SeedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly OcapApiFactory _factory;

    public AiProviderConfigurationsEndpointTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task TestConnection_WithoutAConfiguredProvider_ReturnsNotFoundInsteadOfServerError()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var response = await client.PostAsync($"/api/aiproviderconfigurations/tenant/{SeedTenantId}/test-connection", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Sin proveedor de IA configurado");
    }

    [Fact]
    public async Task ChannelAiAssignment_ValidatesTheProviderOfTheOrganization()
    {
        var client = await AdminClientAsync();
        var created = await client.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = SeedTenantId, providerName = "Claude", displayName = "Claude soporte", modelName = "claude-sonnet-5-5", apiKey = "sk-ant-prueba"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var config = (await created.Content.ReadFromJsonAsync<Config>())!;

        var bot = await client.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Web", widgetTitle = "Hola" });
        bot.IsSuccessStatusCode.Should().BeTrue();
        var connections = await client.GetFromJsonAsync<Envelope>("/api/channels/connections");
        var connectionId = connections!.Data.First().Id;

        (await client.PutAsJsonAsync($"/api/channels/connections/{connectionId}/ai", new { aiProviderConfigId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/api/channels/connections/{connectionId}/ai", new { aiProviderConfigId = config.Id }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<Envelope>("/api/channels/connections"))!.Data.First(c => c.Id == connectionId)
            .ConfigurationMetadata["AiProviderConfigId"].Should().Be(config.Id.ToString());

        (await client.PatchAsync($"/api/aiproviderconfigurations/tenant/{SeedTenantId}/{config.Id}/status?enable=false", null)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/channels/connections/{connectionId}/ai", new { aiProviderConfigId = config.Id }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/api/channels/connections/{connectionId}/ai", new { aiProviderConfigId = (Guid?)null }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    private sealed record Config(Guid Id);
    private sealed record Connection(Guid Id, Dictionary<string, string> ConfigurationMetadata);
    private sealed record Envelope(List<Connection> Data);
    private sealed record Session(string AccessToken);
}
