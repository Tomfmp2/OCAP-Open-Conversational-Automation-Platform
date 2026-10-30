using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public sealed class LoginRateLimitFactory : OcapApiFactory
{
    protected override IDictionary<string, string?> ExtraSettings => new Dictionary<string, string?>
    {
        ["RateLimiting:AuthPermitLimit"] = "3"
    };
}

public class LoginProtectionTests
{
    private const string Password = "ChangeMe_Admin_2026!";

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount_EvenWithTheRightOneAfterwards()
    {
        using var factory = new OcapApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            (await Login(client, "admin@ocap.io", $"incorrecta-{i}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await Login(client, "admin@ocap.io", Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TooManyAttemptsFromTheSameIp_Return429()
    {
        using var factory = new LoginRateLimitFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            (await Login(client, "nadie@ocap.io", "una-contraseña-cualquiera")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var blocked = await Login(client, "admin@ocap.io", Password);
        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        blocked.Headers.Contains("Retry-After").Should().BeTrue();
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });
}
