using System.Net;
using System.Text;
using FluentAssertions;
using OCAP.Intelligence.Application.Services;
using Xunit;

namespace OCAP.Intelligence.Tests;

public class TransientAiRetryHandlerTests
{
    private sealed class Sequence(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(statuses[Bodies.Count - 1]);
        }
    }

    private static HttpClient Client(HttpMessageHandler inner) =>
        new(new TransientAiRetryHandler(inner, [TimeSpan.Zero, TimeSpan.Zero]));

    private static HttpRequestMessage Post() =>
        new(HttpMethod.Post, "https://ia.example.com/") { Content = new StringContent("pregunta", Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Un503Momentaneo_SeReintentaConElMismoCuerpo()
    {
        var inner = new Sequence(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        var response = await Client(inner).SendAsync(Post());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.Bodies.Should().Equal("pregunta", "pregunta");
    }

    [Fact]
    public async Task Un503Persistente_DevuelveElFalloTrasDosReintentos()
    {
        var inner = new Sequence(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable);

        var response = await Client(inner).SendAsync(Post());

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        inner.Bodies.Should().HaveCount(3);
    }

    [Fact]
    public async Task Un429_NoSeReintenta()
    {
        var inner = new Sequence(HttpStatusCode.TooManyRequests);

        var response = await Client(inner).SendAsync(Post());

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Bodies.Should().HaveCount(1);
    }
}
