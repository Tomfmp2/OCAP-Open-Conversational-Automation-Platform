using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Providers.Ollama;

namespace OCAP.Intelligence.Tests;

public class OllamaAiProviderTests
{
    [Fact]
    public async Task GenerateResponseAsync_ReturnsLocalResponse()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { BaseUrl = "http://localhost:11434", ModelName = "llama3" };
        var provider = new OllamaAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Hola Ollama" };

        // Act
        var response = await provider.GenerateResponseAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ProviderName.Should().Be("Ollama");
        response.ModelName.Should().Be("llama3");
    }

    [Fact]
    public async Task StreamResponseAsync_YieldsTokens()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { BaseUrl = "http://localhost:11434" };
        var provider = new OllamaAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Streaming Test Ollama" };

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
