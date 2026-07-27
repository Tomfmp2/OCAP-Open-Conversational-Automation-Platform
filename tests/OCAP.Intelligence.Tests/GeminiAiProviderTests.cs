using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Providers.Gemini;

namespace OCAP.Intelligence.Tests;

public class GeminiAiProviderTests
{
    [Fact]
    public async Task GenerateResponseAsync_WithOfflineSettings_ReturnsSimulatedResponse()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gemini-1.5-flash" };
        var provider = new GeminiAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Hola Gemini" };

        // Act
        var response = await provider.GenerateResponseAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ProviderName.Should().Be("Gemini");
        response.ModelName.Should().Be("gemini-1.5-flash");
        response.GeneratedText.Should().Contain("Hola Gemini");
    }

    [Fact]
    public async Task StreamResponseAsync_YieldsTokens()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key" };
        var provider = new GeminiAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Streaming Test Gemini" };

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamResponseAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
    }
}
