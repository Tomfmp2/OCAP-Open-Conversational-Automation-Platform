using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;
using OCAP.Providers.Gemini;

namespace OCAP.Intelligence.Tests;

/// <summary>Sin keys de IA en el servidor, el asistente responde con Mock en vez de fallar con Gemini sin key.</summary>
public class AiDemoModeTests
{
    private static IAiProvider[] RegisteredProviders() =>
    [
        new GeminiAiProvider(new HttpClient(), new AiProviderSettings { ApiKey = string.Empty }),
        new MockAiProvider()
    ];

    [Fact]
    public async Task AgentSelector_InDemoMode_UsesMock()
    {
        var selector = new DefaultLanguageModelProviderSelector(
            Array.Empty<ILanguageModelProvider>(), RegisteredProviders(), demoMode: new AiDemoMode(true));

        var provider = await selector.GetProviderAsync(Guid.NewGuid());

        provider.ProviderName.Should().Be(AiDemoMode.MockProviderName);
    }

    [Fact]
    public async Task AgentSelector_WithKeys_PrefersTheRealProvider()
    {
        var selector = new DefaultLanguageModelProviderSelector(
            Array.Empty<ILanguageModelProvider>(), RegisteredProviders(), demoMode: new AiDemoMode(false));

        var provider = await selector.GetProviderAsync(Guid.NewGuid());

        provider.ProviderName.Should().Be("Gemini");
    }

    [Fact]
    public async Task AiSelector_InDemoMode_StartsOnMock()
    {
        var selector = new AiProviderSelector(
            RegisteredProviders(),
            new InMemoryAiResponseCache(new MemoryCache(new MemoryCacheOptions())),
            NullLogger<AiProviderSelector>.Instance,
            new AiDemoMode(true));

        var response = await selector.ExecuteWithFailoverAsync(new AiRequest { UserMessage = "Hola" });

        selector.ActiveProviderName.Should().Be(AiDemoMode.MockProviderName);
        response.GeneratedText.Should().Contain("Modo demo").And.Contain("Hola");
    }
}
