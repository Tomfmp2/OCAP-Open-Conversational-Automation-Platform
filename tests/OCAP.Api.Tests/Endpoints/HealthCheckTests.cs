using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

// Pruebas de integración para el endpoint de health check.
// Verifica que la API inicia correctamente y responde en estado saludable.
public class HealthCheckTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;
    private readonly HttpClient _client;

    public HealthCheckTests(OcapApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/health/diagnostic")]
    [InlineData("/api/health/system")]
    public async Task DetailedHealth_RequiresASession(string path)
    {
        (await _client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Metrics_AreOnlyServedToTheInternalNetwork()
    {
        // El TestServer no tiene IP remota: equivale a un cliente que no es la red de Docker.
        (await _client.GetAsync("/metrics")).StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        var admin = TestAuthHelper.CreateAuthenticatedClient(_factory);
        var response = await admin.GetAsync("/api/health/diagnostic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        document.Should().NotBeNull();

        var root = document!.RootElement;
        root.GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("isSystemReady").ValueKind.Should().BeOneOf(
            JsonValueKind.True,
            JsonValueKind.False);
        root.GetProperty("timestamp").GetDateTime().Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromMinutes(1));

        var steps = root.GetProperty("steps");
        steps.GetArrayLength().Should().BeGreaterThanOrEqualTo(4);
        foreach (var step in steps.EnumerateArray())
        {
            step.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
            step.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
            step.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace();
            step.GetProperty("status").GetString().Should().BeOneOf("completed", "error");
            step.GetProperty("details").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }
}
