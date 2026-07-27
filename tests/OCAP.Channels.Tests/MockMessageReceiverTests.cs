using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.Mock;

namespace OCAP.Channels.Tests;

// Pruebas unitarias para MockMessageReceiver.
// Verifica la recepción de mensajes, sanitización y límites de seguridad.
public class MockMessageReceiverTests
{
    private readonly MockMessageReceiver _receiver;

    public MockMessageReceiverTests()
    {
        _receiver = new MockMessageReceiver(NullLogger<MockMessageReceiver>.Instance);
    }

    [Fact]
    public async Task ReceiveMessageAsync_WithValidMessage_ReturnsTrueAndStoresMessage()
    {
        // Arrange
        var message = new IncomingChannelMessage
        {
            ExternalUserId = "user-123",
            Message = "  Hola OCAP  ",
            ChannelName = "Mock"
        };

        // Act
        var result = await _receiver.ReceiveMessageAsync(message);

        // Assert
        result.Should().BeTrue();
        _receiver.ReceivedMessages.Should().HaveCount(1);
        _receiver.ReceivedMessages[0].Message.Should().Be("Hola OCAP");
    }

    [Fact]
    public async Task ReceiveMessageAsync_WithNullMessage_ReturnsFalse()
    {
        // Act
        var result = await _receiver.ReceiveMessageAsync(null!);

        // Assert
        result.Should().BeFalse();
        _receiver.ReceivedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ReceiveMessageAsync_WithEmptyExternalUserId_ReturnsFalse()
    {
        // Arrange
        var message = new IncomingChannelMessage
        {
            ExternalUserId = "   ",
            Message = "Hola sin usuario",
            ChannelName = "Mock"
        };

        // Act
        var result = await _receiver.ReceiveMessageAsync(message);

        // Assert
        result.Should().BeFalse();
        _receiver.ReceivedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ReceiveMessageAsync_ExceedingSizeLimit_ReturnsFalse()
    {
        // Arrange: mensaje que supera el tamaño máximo permitido (10 KB).
        var hugeMessage = new string('A', 11 * 1024);
        var message = new IncomingChannelMessage
        {
            ExternalUserId = "user-123",
            Message = hugeMessage,
            ChannelName = "Mock"
        };

        // Act
        var result = await _receiver.ReceiveMessageAsync(message);

        // Assert
        result.Should().BeFalse();
        _receiver.ReceivedMessages.Should().BeEmpty();
    }
}
