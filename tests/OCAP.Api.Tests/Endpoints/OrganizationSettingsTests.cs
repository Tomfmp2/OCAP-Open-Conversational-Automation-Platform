using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class OrganizationSettingsTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public OrganizationSettingsTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_RenamesTheOrganization()
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory);

        var renamed = await client.PutAsJsonAsync("/api/settings", new { name = "  Tienda Central  " });

        renamed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetFromJsonAsync<Organization>("/api/settings"))!.Name.Should().Be("Tienda Central");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rename_WithoutAName_IsRejected(string name)
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory);

        (await client.PutAsJsonAsync("/api/settings", new { name })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record Organization(string Name);
}
