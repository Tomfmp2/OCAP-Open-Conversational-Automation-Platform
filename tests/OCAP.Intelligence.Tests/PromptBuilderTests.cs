using FluentAssertions;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;
using OCAP.Prompts;

namespace OCAP.Intelligence.Tests;

public class PromptBuilderTests
{
    private readonly SystemPromptBuilder _builder = new();

    [Fact]
    public void BuildPrompt_WithValidAgent_GeneratesSystemAndUserPrompts()
    {
        // Arrange
        var config = new AgentConfiguration("Eres una secretaria virtual profesional.");
        var agent = new Agent(Guid.NewGuid(), new AgentName("SecretariaIA"), "Agente de prueba", config);
        var userMessage = "Crear reunión para mañana";

        // Act
        var template = _builder.BuildPrompt(agent, userMessage, null, null);

        // Assert
        template.Should().NotBeNull();
        template.RenderSystemPrompt().Should().Contain("secretaria virtual profesional");
        template.RenderUserPrompt().Should().Be("Crear reunión para mañana");
    }
}
