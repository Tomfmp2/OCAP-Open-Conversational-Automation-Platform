using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Tests.Endpoints;

public class MessagesEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;
    private readonly OcapApiFactory _factory;

    public MessagesEndpointTests(OcapApiFactory factory)
    {
        _factory = factory;
        _client = TestAuthHelper.CreateAuthenticatedClient(factory);
    }

    [Fact]
    public async Task PostMessage_WithValidRequest_Returns200Ok()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);

        var request = new
        {
            UserId = userId,
            MessageContent = "Hola OCAP",
            Provider = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/messages", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostMessage_WithEmptyContent_Returns400BadRequest()
    {
        var request = new
        {
            UserId = Guid.NewGuid(),
            MessageContent = "",
            Provider = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/messages", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMessage_WithMissingUserId_Returns400BadRequest()
    {
        var request = new
        {
            MessageContent = "Hola sin usuario",
            Provider = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/messages", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMessage_WithNonExistentUser_ReturnsError()
    {
        var request = new
        {
            UserId = Guid.NewGuid(),
            MessageContent = "Mensaje de usuario inexistente",
            Provider = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/messages", request);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.InternalServerError);
    }

    private async Task SeedUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var user = new User(userId, "Test User", tenantId);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
