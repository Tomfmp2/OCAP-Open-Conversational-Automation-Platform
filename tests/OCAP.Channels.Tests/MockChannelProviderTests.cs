using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Channels.Mock;

namespace OCAP.Channels.Tests;

// Pruebas unitarias para MockChannelProvider.
// Verifica la inicialización del proveedor, metadatos y control del ciclo de vida.
public class MockChannelProviderTests
{
    private readonly MockChannelProvider _provider;

    public MockChannelProviderTests()
    {
        var receiverLogger = NullLogger<MockMessageReceiver>.Instance;
        var senderLogger = NullLogger<MockMessageSender>.Instance;
        var providerLogger = NullLogger<MockChannelProvider>.Instance;

        var receiver = new MockMessageReceiver(receiverLogger);
        var sender = new MockMessageSender(senderLogger);

        _provider = new MockChannelProvider(providerLogger, receiver, sender);
    }

    [Fact]
    public void Metadata_OnInit_HasCorrectInitialState()
    {
        // Assert
        _provider.Metadata.ChannelName.Should().Be("Mock");
        _provider.Metadata.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_WhenCalled_SetsIsEnabledToTrue()
    {
        // Act
        await _provider.InitializeAsync();

        // Assert
        _provider.Metadata.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task StopAsync_WhenCalled_SetsIsEnabledToFalse()
    {
        // Arrange
        await _provider.InitializeAsync();

        // Act
        await _provider.StopAsync();

        // Assert
        _provider.Metadata.IsEnabled.Should().BeFalse();
    }
}
