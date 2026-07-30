using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Api.Tests.Endpoints;

public class AuthorizationPipelineTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationPipelineTests(OcapApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidJwt_Returns200Ok()
    {
        var token = CreateAccessToken();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicHealthEndpoint_WithoutToken_Returns200Ok()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private string CreateAccessToken()
    {
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var tenant = new Tenant(tenantId, "Test Tenant", "test-tenant");
        var user = new UserIdentity(Guid.NewGuid(), tenantId, "auth-pipeline@ocap.test", "hash", "salt", "Auth Pipeline");
        var role = new Role(Guid.NewGuid(), tenantId, "Admin", "Admin", new[] { "Conversation.Read" });

        return jwt.GenerateAccessToken(user, tenant, role, role.Permissions);
    }
}
