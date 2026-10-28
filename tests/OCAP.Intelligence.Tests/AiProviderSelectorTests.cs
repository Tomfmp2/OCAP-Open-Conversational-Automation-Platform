using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;
using Moq;
using OCAP.Providers.OpenAI;

namespace OCAP.Intelligence.Tests;

public class AiProviderSelectorTests
{
    [Fact]
    public async Task ExecuteWithFailoverAsync_SelectsActiveProviderAndReturnsResponse()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var openAiSettings = new AiProviderSettings { ApiKey = "mock-key" };
        var openAi = new OpenAiProvider(httpClient, openAiSettings);
        var mockAi = new Mock<IAiProvider>();
        mockAi.Setup(p => p.Name).Returns("MockAI");
        mockAi.Setup(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AiResponse { ProviderName = "MockAI", GeneratedText = "Response from Mock" });

        var selector = new AiProviderSelector(new IAiProvider[] { openAi, mockAi.Object }, NullLogger<AiProviderSelector>.Instance);
        var request = new AiRequest { UserMessage = "Prueba Selector Failover" };

        // Act
        var response = await selector.ExecuteWithFailoverAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ProviderName.Should().Be("MockAI");
    }

    [Fact]
    public async Task ExecuteWithFailoverAsync_SameMessageTwice_CallsProviderEachTime()
    {
        // Dos organizaciones pueden enviar el mismo texto con instrucciones y conocimiento distintos: nunca se
        // reutiliza una respuesta anterior.
        var mockAi = new Mock<IAiProvider>();
        mockAi.Setup(p => p.Name).Returns("Gemini");
        mockAi.SetupSequence(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AiResponse { ProviderName = "Gemini", GeneratedText = "respuesta A" })
              .ReturnsAsync(new AiResponse { ProviderName = "Gemini", GeneratedText = "respuesta B" });
        var selector = new AiProviderSelector(new IAiProvider[] { mockAi.Object }, NullLogger<AiProviderSelector>.Instance);

        var first = await selector.ExecuteWithFailoverAsync(new AiRequest { UserMessage = "¿Cuál es mi saldo?", SystemInstructions = "Tenant A" });
        var second = await selector.ExecuteWithFailoverAsync(new AiRequest { UserMessage = "¿Cuál es mi saldo?", SystemInstructions = "Tenant B" });

        first.GeneratedText.Should().Be("respuesta A");
        second.GeneratedText.Should().Be("respuesta B");
        mockAi.Verify(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
