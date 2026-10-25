using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;

namespace OCAP.Agents.Tests;

/// <summary>El idioma que se elige en `ocap install` (Assistant:Language) define en qué responde el asistente del panel.</summary>
public class AssistantLanguageTests
{
    [Theory]
    [InlineData(null, "Responde en español")]
    [InlineData("es", "Responde en español")]
    [InlineData("en", "Responde en inglés")]
    public async Task SystemPrompt_UsesTheConfiguredLanguage(string? language, string expected)
    {
        LanguageModelRequest? captured = null;
        var provider = new Mock<ILanguageModelProvider>();
        provider.Setup(p => p.GenerateAsync(It.IsAny<LanguageModelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LanguageModelRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new LanguageModelResponse("ok", "Fake"));
        var selector = new Mock<ILanguageModelProviderSelector>();
        selector.Setup(s => s.GetProviderAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider.Object);
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Assistant:Language"]).Returns(language);

        var agent = new EnterpriseAssistantAgent(
            selector.Object,
            new ToolRegistry(),
            Mock.Of<IActionDispatcher>(),
            Mock.Of<IOcapSystemContextProvider>(c =>
                c.GetTenantSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()) == Task.FromResult(string.Empty)),
            configuration.Object,
            NullLogger<EnterpriseAssistantAgent>.Instance);

        await agent.ProcessRequestAsync(new AgentContext(agent.GlobalAgentId, Guid.NewGuid(), Guid.NewGuid(), "Hola"));

        captured!.Messages[0].Content.Should().Contain(expected).And.NotContain("{{LANGUAGE}}");
    }
}
