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
        var token = CreateAccessToken("Security.Manage");
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

    [Theory]
    [InlineData("GET", "/api/roles")]
    [InlineData("GET", "/api/users")]
    [InlineData("POST", "/api/tenants")]
    [InlineData("GET", "/api/tenants/00000000-0000-0000-0000-000000000001/members")]
    [InlineData("GET", "/api/apikeys")]
    [InlineData("GET", "/api/webhooks")]
    [InlineData("POST", "/api/channels/webchat/connect")]
    [InlineData("DELETE", "/api/channels/00000000-0000-0000-0000-000000000009")]
    [InlineData("POST", "/api/agents")]
    [InlineData("GET", "/api/aiproviderconfigurations/tenant/00000000-0000-0000-0000-000000000001")]
    [InlineData("PUT", "/api/settings")]
    [InlineData("POST", "/api/knowledge")]
    public async Task Operator_CannotAdministerTheOrganization(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "GET" ? null : new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateAccessToken(OCAP.Api.Security.OcapPermissions.Operator.ToArray()));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/inbox")]
    [InlineData("/api/agents")]
    [InlineData("/api/dashboard/overview")]
    [InlineData("/api/settings")]
    [InlineData("/api/tenants")]
    public async Task Operator_CanUseTheInboxAndReadTheOrganization(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateAccessToken(OCAP.Api.Security.OcapPermissions.Operator.ToArray()));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tenants_OnlyExposeTheOwnOrganization()
    {
        var admin = CreateAccessToken("Security.Manage");
        var other = Guid.NewGuid();

        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = body is null ? null : System.Net.Http.Json.JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            return await _client.SendAsync(request);
        }

        var list = await (await Send(HttpMethod.Get, "/api/tenants")).Content.ReadAsStringAsync();
        list.Should().Contain("00000000-0000-0000-0000-000000000001");
        System.Text.Json.JsonDocument.Parse(list).RootElement.GetArrayLength().Should().Be(1);

        (await Send(HttpMethod.Get, $"/api/tenants/{other}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Send(HttpMethod.Get, $"/api/tenants/{other}/members")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Send(HttpMethod.Post, $"/api/tenants/{other}/members", new { userId = Guid.NewGuid(), role = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Send(HttpMethod.Post, "/api/tenants", new { name = "Ajena", slug = "ajena" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private string CreateAccessToken(params string[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var tenant = new Tenant(tenantId, "Test Tenant", "test-tenant");
        var user = new UserIdentity(Guid.NewGuid(), tenantId, "auth-pipeline@ocap.test", "hash", "salt", "Auth Pipeline");
        var role = new Role(Guid.NewGuid(), tenantId, "Operador", "Operador", permissions.Length > 0 ? permissions : ["Conversation.Read"]);

        return jwt.GenerateAccessToken(user, tenant, role, role.Permissions);
    }
}
