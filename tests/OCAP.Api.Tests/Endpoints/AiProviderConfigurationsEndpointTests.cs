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

    private sealed record Session(string AccessToken);
}
