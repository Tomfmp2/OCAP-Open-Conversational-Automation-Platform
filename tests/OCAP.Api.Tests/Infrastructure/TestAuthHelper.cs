using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Api.Tests.Infrastructure;

public static class TestAuthHelper
{
    public static string CreateAccessToken(
        OcapApiFactory factory,
        string email = "rc-test@ocap.test",
        string roleName = "Admin",
        IEnumerable<string>? permissions = null)
    {
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var tenant = new Tenant(tenantId, "Test Tenant", "test-tenant");
        var user = new UserIdentity(Guid.NewGuid(), tenantId, email, "hash", "salt", "RC Test User");
        var role = new Role(
            Guid.NewGuid(),
            tenantId,
            roleName,
            roleName,
            permissions?.ToArray() ?? ["Conversation.Read", "Conversation.Write", "Admin.Full"]);

        return jwt.GenerateAccessToken(user, tenant, role, role.Permissions);
    }

    public static HttpClient CreateAuthenticatedClient(OcapApiFactory factory, string? token = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token ?? CreateAccessToken(factory));
        return client;
    }
}
