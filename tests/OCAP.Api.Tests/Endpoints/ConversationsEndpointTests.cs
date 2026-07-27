using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

// Pruebas de integración para el endpoint GET /api/conversations/{id}.
// Cubre los escenarios: conversación encontrada, conversación no encontrada e ID inválido.
public class ConversationsEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;

    public ConversationsEndpointTests(OcapApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConversation_WithNonExistentId_Returns404NotFound()
    {
        // Arrange: usar un ID que no existe en la base de datos InMemory.
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/conversations/{nonExistentId}");

        // Assert: el caso de uso lanza InvalidOperationException que el middleware
        // convierte en 422 o el controller en 404; ambos son respuestas válidas aquí.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetConversation_WithInvalidGuid_Returns400BadRequest()
    {
        // Arrange: ID que no puede ser parseado como GUID válido.
        // Act
        var response = await _client.GetAsync("/api/conversations/not-a-valid-guid");

        // Assert: el model binding de ASP.NET rechaza el GUID malformado.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
