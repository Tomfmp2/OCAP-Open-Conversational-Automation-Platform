using System.Net;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

// Pruebas de integración para el endpoint de health check.
// Verifica que la API inicia correctamente y responde en estado saludable.
public class HealthCheckTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(OcapApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_WhenApiIsRunning_Returns200Ok()
    {
        // Act: consultar el endpoint de health expuesto por ASP.NET Health Checks.
        var response = await _client.GetAsync("/api/health");

        // Assert: la API debe responder saludablemente cuando todos los servicios están disponibles.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthController_WhenApiIsRunning_Returns200Ok()
    {
        // Act: consultar el endpoint del HealthController personalizado.
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // El cuerpo debe ser un JSON válido (no vacío).
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
