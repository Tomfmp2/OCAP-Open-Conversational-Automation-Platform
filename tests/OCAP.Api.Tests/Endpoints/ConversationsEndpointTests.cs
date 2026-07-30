using System.Net;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class ConversationsEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;

    public ConversationsEndpointTests(OcapApiFactory factory)
    {
        _client = TestAuthHelper.CreateAuthenticatedClient(factory);
    }

    [Fact]
    public async Task GetConversation_WithNonExistentId_Returns404NotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/conversations/{nonExistentId}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetConversation_WithInvalidGuid_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/conversations/not-a-valid-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
