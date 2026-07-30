using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Providers.OpenAI;

namespace OCAP.Intelligence.Tests;

public class OpenAiProviderTests
{
    [Fact]
    public async Task GenerateResponseAsync_WithoutValidApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gpt-4o" };
        var provider = new OpenAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Hola OpenAI" };

        // Act & Assert
        var act = async () => await provider.GenerateResponseAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StreamResponseAsync_WithoutValidApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gpt-4o" };
        var provider = new OpenAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Streaming Test" };

        // Act & Assert
        var act = async () =>
        {
            await foreach (var _ in provider.StreamResponseAsync(request)) { }
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HealthAsync_ReturnsHealthInfo()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key" };
        var provider = new OpenAiProvider(httpClient, settings);

        // Act
        var health = await provider.HealthAsync();

        // Assert
        health.Should().NotBeNull();
        health.ProviderName.Should().Be("OpenAI");
    }
}
