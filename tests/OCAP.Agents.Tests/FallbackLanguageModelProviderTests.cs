using System.Net;
using FluentAssertions;
using Moq;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;
using Xunit;

namespace OCAP.Agents.Tests;

public class FallbackLanguageModelProviderTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly LanguageModelRequest Request = new([PromptMessage.User("hola")]);

    private static Mock<IAiProvider> Model(string name)
    {
        var provider = new Mock<IAiProvider>();
        provider.SetupGet(p => p.Name).Returns("Gemini");
        provider.Setup(p => p.GetModelInformation()).Returns(new AiModelInformation { Provider = "Gemini", Model = name });
        provider.Setup(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse { GeneratedText = $"respondió {name}", ProviderName = "Gemini", ModelName = name });
        return provider;
    }

    private static Mock<IAiProvider> Failing(string name, Exception error)
    {
        var provider = Model(name);
        provider.Setup(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
        return provider;
    }

    private static Mock<IAiProviderConfigurationService> Configurations(IAiProvider? fallback)
    {
        var configurations = new Mock<IAiProviderConfigurationService>();
        configurations.Setup(c => c.GetFallbackProviderAsync(Tenant, "Gemini", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallback);
        return configurations;
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SiElModeloFalla_RespondeOtroModeloDeLaOrganizacion(HttpStatusCode status)
    {
        var primary = Failing("flash", new AiProviderException("Gemini", "flash", status, "detalle"));
        var sut = new FallbackLanguageModelProvider(primary.Object, Tenant, Configurations(Model("lite").Object).Object);

        var response = await sut.GenerateAsync(Request);

        response.Content.Should().Be("respondió lite");
    }

    [Fact]
    public async Task SinOtroModelo_ConservaElErrorOriginal()
    {
        var primary = Failing("flash", new AiProviderException("Gemini", "flash", HttpStatusCode.TooManyRequests, "detalle"));
        var sut = new FallbackLanguageModelProvider(primary.Object, Tenant, Configurations(null).Object);

        var act = () => sut.GenerateAsync(Request);

        await act.Should().ThrowAsync<AiProviderException>();
    }

    [Fact]
    public async Task UnaPeticionInvalida_NoGastaOtroModelo()
    {
        var primary = Failing("flash", new AiProviderException("Gemini", "flash", HttpStatusCode.BadRequest, "detalle"));
        var configurations = Configurations(Model("lite").Object);
        var sut = new FallbackLanguageModelProvider(primary.Object, Tenant, configurations.Object);

        var act = () => sut.GenerateAsync(Request);

        await act.Should().ThrowAsync<AiProviderException>();
        configurations.Verify(c => c.GetFallbackProviderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnModeloLento_CedeElTurnoAlVencerSuPlazo()
    {
        var primary = Model("lento");
        primary.Setup(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (AiRequest _, CancellationToken ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new AiResponse();
            });
        var sut = new FallbackLanguageModelProvider(primary.Object, Tenant, Configurations(Model("lite").Object).Object, attemptTimeout: TimeSpan.FromMilliseconds(50));

        var response = await sut.GenerateAsync(Request);

        response.Content.Should().Be("respondió lite");
    }

    [Fact]
    public async Task SiElClienteCancela_NoSeLlamaAOtroModelo()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var primary = Failing("flash", new OperationCanceledException(cancelled.Token));
        var configurations = Configurations(Model("lite").Object);
        var sut = new FallbackLanguageModelProvider(primary.Object, Tenant, configurations.Object);

        var act = () => sut.GenerateAsync(Request, cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        configurations.Verify(c => c.GetFallbackProviderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
