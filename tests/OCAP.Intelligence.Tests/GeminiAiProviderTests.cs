using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Providers.Gemini;

namespace OCAP.Intelligence.Tests;

public class GeminiAiProviderTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test code httpClient cleanup handled by xUnit")]
    public async Task GenerateResponseAsync_WithoutValidApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gemini-1.5-flash" };
        var provider = new GeminiAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Hola Gemini" };

        // Act & Assert
        var act = () => provider.GenerateResponseAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No se ha configurado una API Key válida*");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test code httpClient cleanup handled by xUnit")]
    public async Task StreamResponseAsync_WithoutValidApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var settings = new AiProviderSettings { ApiKey = "mock-key" };
        var provider = new GeminiAiProvider(httpClient, settings);
        var request = new AiRequest { UserMessage = "Streaming Test Gemini" };

        // Act & Assert
        var act = async () =>
        {
            await foreach (var _ in provider.StreamResponseAsync(request))
            {
            }
        };
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
