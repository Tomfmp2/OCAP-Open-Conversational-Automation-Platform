using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Providers.OpenAI;

namespace OCAP.Intelligence.Tests;

public class OpenAiProviderTests
{
    [Fact]
    public async Task GenerateResponseAsync_WithOfflineSettings_ReturnsSimulatedResponse()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gpt-4o" };
        var provider = new OpenAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Hola OpenAI" };

        // Act
        var response = await provider.GenerateResponseAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ProviderName.Should().Be("OpenAI");
        response.ModelName.Should().Be("gpt-4o");
        response.GeneratedText.Should().Contain("Hola OpenAI");
    }

    [Fact]
    public async Task StreamResponseAsync_YieldsTokens()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gpt-4o" };
        var provider = new OpenAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Streaming Test" };

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamResponseAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
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
