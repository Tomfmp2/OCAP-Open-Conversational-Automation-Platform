using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.Mock;

namespace OCAP.Channels.Tests;

// Pruebas unitarias para MockMessageSender.
// Verifica el despacho de respuestas hacia destinatarios simulados.
public class MockMessageSenderTests
{
    private readonly MockMessageSender _sender;

    public MockMessageSenderTests()
    {
        _sender = new MockMessageSender(NullLogger<MockMessageSender>.Instance);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidDestination_ReturnsTrueAndStoresMessage()
    {
        // Arrange
        var message = new OutgoingChannelMessage
        {
            DestinationUserId = "user-123",
            Message = "Respuesta simulada",
            ChannelName = "Mock"
        };

        // Act
        var result = await _sender.SendMessageAsync(message);

        // Assert
        result.Should().BeTrue();
        _sender.SentMessages.Should().HaveCount(1);
        _sender.SentMessages[0].Message.Should().Be("Respuesta simulada");
    }

    [Fact]
    public async Task SendMessageAsync_WithNullMessage_ReturnsFalse()
    {
        // Act
        var result = await _sender.SendMessageAsync(null!);

        // Assert
        result.Should().BeFalse();
        _sender.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyDestinationUserId_ReturnsFalse()
    {
        // Arrange
        var message = new OutgoingChannelMessage
        {
            DestinationUserId = "",
            Message = "Hola sin destino",
            ChannelName = "Mock"
        };

        // Act
        var result = await _sender.SendMessageAsync(message);

        // Assert
        result.Should().BeFalse();
        _sender.SentMessages.Should().BeEmpty();
    }
}
