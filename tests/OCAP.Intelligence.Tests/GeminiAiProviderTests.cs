using System.Net;
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

public class GeminiAiProviderResponseTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private static GeminiAiProvider Provider(HttpStatusCode status, string body) =>
        new(new HttpClient(new StubHandler(status, body)), new AiProviderSettings { ApiKey = "clave-de-prueba-valida", ModelName = "gemini-prueba" });

    [Fact]
    public async Task Tokens_SonLosQueInformaElProveedor()
    {
        var provider = Provider(HttpStatusCode.OK, """
            {"candidates":[{"content":{"parts":[{"text":"hola"}]}}],"usageMetadata":{"totalTokenCount":437}}
            """);

        var response = await provider.GenerateResponseAsync(new AiRequest { UserMessage = "hola" });

        response.TokensUsed.Should().Be(437);
    }

    [Fact]
    public async Task SinDatosDeUso_NoInventaTokens()
    {
        var provider = Provider(HttpStatusCode.OK, """{"candidates":[{"content":{"parts":[{"text":"hola"}]}}]}""");

        var response = await provider.GenerateResponseAsync(new AiRequest { UserMessage = "hola" });

        response.TokensUsed.Should().Be(0);
    }

    [Fact]
    public async Task CuotaAgotada_DaUnMensajeClaroYElPlazoDeEspera()
    {
        var provider = Provider(HttpStatusCode.TooManyRequests, """
            {"error":{"code":429,"message":"secreto-del-proveedor","details":[{"retryDelay":"58964s"}]}}
            """);

        var act = () => provider.GenerateResponseAsync(new AiRequest { UserMessage = "hola" });

        var ex = (await act.Should().ThrowAsync<AiProviderException>()).Which;
        ex.IsQuotaExceeded.Should().BeTrue();
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(58964));
        ex.Message.Should().Contain("agotó su cuota").And.NotContain("secreto-del-proveedor");
        ex.ToString().Should().Contain("secreto-del-proveedor");
    }
}
