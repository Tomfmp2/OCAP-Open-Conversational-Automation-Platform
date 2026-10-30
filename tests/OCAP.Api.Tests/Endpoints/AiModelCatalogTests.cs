using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class AiModelCatalogTests : IClassFixture<OcapApiFactory>
{
    private static readonly Guid SeedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly OcapApiFactory _factory;

    public AiModelCatalogTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SeveralOpenAiCompatibleConfigurations_CanCoexist_AndOneBecomesTheDefault()
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory);
        object Compatible(string name, string? baseUrl) => new
        {
            tenantId = SeedTenantId, providerName = "OpenAICompatible", displayName = name, modelName = "meta/llama-4", apiKey = "nvapi-prueba", baseUrl
        };

        (await client.PostAsJsonAsync("/api/aiproviderconfigurations", Compatible("Sin URL", null))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var first = await client.PostAsJsonAsync("/api/aiproviderconfigurations", Compatible("NVIDIA", "https://8.8.8.8/v1"));
        var second = await client.PostAsJsonAsync("/api/aiproviderconfigurations", Compatible("Groq", "https://8.8.4.4/openai/v1"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var groq = (await second.Content.ReadFromJsonAsync<Config>())!;

        (await client.PostAsJsonAsync("/api/providers/select", new { providerName = groq.Id.ToString() })).EnsureSuccessStatusCode();

        var providers = await client.GetFromJsonAsync<List<ProviderInfo>>("/api/providers");
        providers!.Where(p => p.IsActive).Select(p => p.Id).Should().Equal(groq.Id);
    }

    [Fact]
    public async Task ListingModels_RejectsAnInternalBaseUrl()
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory);

        var response = await client.PostAsJsonAsync("/api/aiproviderconfigurations/models", new
        {
            providerName = "OpenAICompatible", apiKey = "x", baseUrl = "http://169.254.169.254/v1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record Config(Guid Id);
    private sealed record ProviderInfo(string Name, bool IsActive, Guid? Id);
}
