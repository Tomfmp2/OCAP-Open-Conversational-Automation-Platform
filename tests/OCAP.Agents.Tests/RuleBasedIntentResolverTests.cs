using FluentAssertions;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Tests;

// Pruebas unitarias para RuleBasedIntentResolver.
public class RuleBasedIntentResolverTests
{
    private readonly RuleBasedIntentResolver _resolver = new();

    [Theory]
    [InlineData("Hola buenos días", Intent.Greeting)]
    [InlineData("Necesito recordar pagar la factura", Intent.CreateReminder)]
    [InlineData("Quiero hablar con un asesor humano", Intent.HumanSupport)]
    [InlineData("Dame informacion del sistema", Intent.GetInformation)]
    [InlineData("xyzabc123456", Intent.Unknown)]
    public async Task ResolveIntentAsync_ResolvesExpectedIntent(string message, string expectedIntentName)
    {
        // Act
        var intent = await _resolver.ResolveIntentAsync(message, null);

        // Assert
        intent.Name.Should().Be(expectedIntentName);
    }
}
