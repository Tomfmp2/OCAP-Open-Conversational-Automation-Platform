using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Providers.Ollama;

namespace OCAP.Intelligence.Tests;

public class OllamaAiProviderTests
{
    [Fact]
    public async Task GenerateResponseAsync_WhenServerUnreachable_ThrowsInvalidOperationException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { BaseUrl = "http://localhost:11434", ModelName = "llama3" };
        var provider = new OllamaAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Hola Ollama" };

        // Act & Assert
        var act = async () => await provider.GenerateResponseAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StreamResponseAsync_WhenServerUnreachable_ThrowsInvalidOperationException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var settings = new AiProviderSettings { BaseUrl = "http://localhost:11434" };
        var provider = new OllamaAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Streaming Test Ollama" };

        // Act & Assert
        var act = async () =>
        {
            await foreach (var _ in provider.StreamResponseAsync(request)) { }
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
