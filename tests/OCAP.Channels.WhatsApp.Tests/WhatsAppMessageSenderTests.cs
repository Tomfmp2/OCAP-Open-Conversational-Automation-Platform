using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Channels.WhatsApp.Services;

namespace OCAP.Channels.WhatsApp.Tests;

// Pruebas unitarias para WhatsAppMessageSender con mock de llamadas HTTP a Evolution API.
public class WhatsAppMessageSenderTests
{
    [Fact]
    public async Task SendMessageAsync_WhenApiReturns200_ReturnsTrue()
    {
        // Arrange: simular handler HTTP que devuelve 200 OK.
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"SUCCESS\"}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var settings = Options.Create(new WhatsAppSettings
        {
            BaseUrl = "http://localhost:8080",
            Instance = "test-instance",
            ApiKey = "test-key"
        });

        var apiClient = new EvolutionApiClient(httpClient, settings, NullLogger<EvolutionApiClient>.Instance);
        var sender = new WhatsAppMessageSender(apiClient, NullLogger<WhatsAppMessageSender>.Instance);

        var outgoingMessage = new OutgoingChannelMessage
        {
            DestinationUserId = "573001234567",
            Message = "Hola respuesta de prueba",
            ChannelName = "WhatsApp"
        };

        // Act
        var result = await sender.SendMessageAsync(outgoingMessage);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_WhenApiReturns500_ReturnsFalse()
    {
        // Arrange: simular error HTTP 500 en Evolution API.
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Error interno de Evolution API")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var settings = Options.Create(new WhatsAppSettings
        {
            BaseUrl = "http://localhost:8080",
            Instance = "test-instance"
        });

        var apiClient = new EvolutionApiClient(httpClient, settings, NullLogger<EvolutionApiClient>.Instance);
        var sender = new WhatsAppMessageSender(apiClient, NullLogger<WhatsAppMessageSender>.Instance);

        var outgoingMessage = new OutgoingChannelMessage
        {
            DestinationUserId = "573001234567",
            Message = "Hola prueba error",
            ChannelName = "WhatsApp"
        };

        // Act
        var result = await sender.SendMessageAsync(outgoingMessage);

        // Assert
        result.Should().BeFalse();
    }
}
