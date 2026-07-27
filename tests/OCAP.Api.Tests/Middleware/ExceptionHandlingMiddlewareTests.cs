using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Middleware;

// Pruebas de integración que verifican el comportamiento del ExceptionHandlingMiddleware.
// Se prueba a través de endpoints reales que generan excepciones controladas.
public class ExceptionHandlingMiddlewareTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingMiddlewareTests(OcapApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Api_WhenRequestIsValid_DoesNotReturnServerError()
    {
        // Act: el health check nunca debería devolver 500 bajo condiciones normales.
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Api_WhenBodyIsInvalidJson_Returns400BadRequest()
    {
        // Arrange: enviar JSON malformado para provocar un error de deserialización.
        var content = new StringContent("{ invalid json }", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/messages", content);

        // Assert: ASP.NET debe rechazar el JSON malformado con 400.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Api_WhenRouteDoesNotExist_Returns404NotFound()
    {
        // Act: ruta que no está registrada en el gateway.
        var response = await _client.GetAsync("/api/ruta-que-no-existe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
