using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Tests.Endpoints;

// Pruebas de integración para el endpoint POST /api/messages.
// El flujo completo recorre: HTTP → Controller → UseCase → Repository (InMemory).
public class MessagesEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;
    private readonly OcapApiFactory _factory;

    public MessagesEndpointTests(OcapApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostMessage_WithValidRequest_Returns200Ok()
    {
        // Arrange: crear usuario en la base de datos InMemory para que el caso de uso lo encuentre.
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);

        var request = new
        {
            UserId = userId,
            MessageContent = "Hola OCAP",
            Provider = "Test"
        };

        // Act: realizar la petición HTTP real a través del pipeline de ASP.NET Core.
        var response = await _client.PostAsJsonAsync("/api/messages", request);

        // Assert: verificar que la respuesta es 200 OK.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostMessage_WithEmptyContent_Returns400BadRequest()
    {
        // Arrange: mensaje con contenido vacío que debe fallar validación del modelo.
        var request = new
        {
            UserId = Guid.NewGuid(),
            MessageContent = "",
            Provider = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages", request);

        // Assert: el modelo está decorado con [Required], debe rechazar el contenido vacío.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMessage_WithMissingUserId_Returns400BadRequest()
    {
        // Arrange: petición incompleta sin UserId obligatorio.
        var request = new
        {
            MessageContent = "Hola sin usuario",
            Provider = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMessage_WithNonExistentUser_ReturnsError()
    {
        // Arrange: usuario que no existe en la base de datos InMemory.
        // El caso de uso lanza InvalidOperationException que el middleware convierte en 422.
        var request = new
        {
            UserId = Guid.NewGuid(),
            MessageContent = "Mensaje de usuario inexistente",
            Provider = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages", request);

        // Assert: el middleware debe capturar el error y devolver respuesta estructurada.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.InternalServerError);
    }

    // Siembra un usuario activo en la base de datos InMemory para los tests que lo requieren.
    private async Task SeedUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        // El constructor de User requiere (Guid id, string displayName, Guid tenantId).
        // Debe coincidir con el tenant por defecto de HttpTenantContext en Testing anónimo.
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var user = new User(userId, "Test User", tenantId);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
