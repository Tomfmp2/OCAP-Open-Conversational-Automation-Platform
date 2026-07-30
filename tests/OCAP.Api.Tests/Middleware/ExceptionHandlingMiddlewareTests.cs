using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;
    private readonly HttpClient _client;

    public ExceptionHandlingMiddlewareTests(OcapApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Api_WhenRequestIsValid_DoesNotReturnServerError()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Api_WhenBodyIsInvalidJson_Returns400BadRequest()
    {
        var content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/messages")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestAuthHelper.CreateAccessToken(_factory));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Api_WhenRouteDoesNotExist_Returns404NotFound()
    {
        var response = await _client.GetAsync("/api/ruta-que-no-existe");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Api_PropagatesCorrelationIdInResponseHeaders()
    {
        const string correlation = "corr-rc-mega-sprint-1";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlation);

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.Should().Contain(correlation);
    }
}
