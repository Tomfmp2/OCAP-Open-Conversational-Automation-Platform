using FluentAssertions;
using OCAP.Api.Services;
using OCAP.Api.Services.Memory;

namespace OCAP.Api.Tests.Services;

public class MemoryModelTests
{
    [Fact]
    public void Extraction_IsReadEvenInsideAFence_AndSensitiveFactsAreDropped()
    {
        var raw = """
            ```json
            {"episode":"Pidió 2 docenas de empanadas para el viernes.",
             "facts":[{"key":"ciudad","text":"Vive en Medellín"},
                      {"key":"tarjeta","text":"Paga con 4111 1111 1111 1111"},
                      {"key":"acceso","text":"Su contraseña es sol123"}]}
            ```
            """;

        var parsed = MemoryModel.Parse(raw)!;

        parsed.Episode.Should().Be("Pidió 2 docenas de empanadas para el viernes.");
        parsed.Facts.Should().ContainSingle().Which.Should().Be(new ExtractedFact("ciudad", "Vive en Medellín"));
    }

    [Fact]
    public void InvalidOutput_IsNotAnExtraction() => MemoryModel.Parse("no es json").Should().BeNull();

    [Theory]
    [InlineData("olvídame", true)]
    [InlineData("Por favor borra mis datos", true)]
    [InlineData("Forget me", true)]
    [InlineData("hola, ¿tienen empanadas?", false)]
    [InlineData("Una vez un cliente me dijo olvídame y luego volvió a escribir pidiendo tres docenas más", false)]
    public void ForgetRequests_AreShortAndExplicit(string text, bool expected) =>
        ChannelConversationService.IsForgetRequest(text).Should().Be(expected);
}
