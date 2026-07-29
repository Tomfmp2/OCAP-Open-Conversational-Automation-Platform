using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
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

        var cache = new InMemoryAiResponseCache(new MemoryCache(new MemoryCacheOptions()));
        var selector = new AiProviderSelector(new IAiProvider[] { openAi, mockAi.Object }, cache, NullLogger<AiProviderSelector>.Instance);
        var request = new AiRequest { UserMessage = "Prueba Selector Failover" };

        // Act
        var response = await selector.ExecuteWithFailoverAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public async Task SetActiveProvider_ChangesProviderSelection()
    {
        // Arrange
        var mockAi = new Mock<IAiProvider>();
        mockAi.Setup(p => p.Name).Returns("MockAI");
        var cache = new InMemoryAiResponseCache(new MemoryCache(new MemoryCacheOptions()));
        var selector = new AiProviderSelector(new IAiProvider[] { mockAi.Object }, cache, NullLogger<AiProviderSelector>.Instance);

        // Act
        selector.SetActiveProvider("MockAI");

        // Assert
        selector.ActiveProviderName.Should().Be("MockAI");
        var selected = await selector.SelectProviderAsync();
        selected.Name.Should().Be("MockAI");
    }
}
