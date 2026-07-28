using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Application.UseCases;
using Xunit;

namespace OCAP.Agents.Tests;

public class AgentRuntimeAndEnterpriseAssistantTests
{
    [Fact]
    public async Task EnterpriseAssistantAgent_Should_Process_Request_Using_LanguageModelProvider()
    {
        // Arrange
        var mockProvider = new MockLanguageModelProvider("OpenAI", "Respuesta de Inteligencia Empresarial OCAP");
        var selector = new SingleProviderSelector(mockProvider);
        var agent = new EnterpriseAssistantAgent(selector, NullLogger<EnterpriseAssistantAgent>.Instance);

        var context = new AgentContext(
            EnterpriseAssistantAgent.EnterpriseAssistantAgentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "¿Cuál es el estado del flujo de trabajo?");

        // Act
        var result = await agent.ProcessRequestAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.OutputMessage.Should().Be("Respuesta de Inteligencia Empresarial OCAP");
        result.AgentId.Should().Be(EnterpriseAssistantAgent.EnterpriseAssistantAgentId);
        result.ProviderUsed.Should().Be("OpenAI");
    }

    [Fact]
    public async Task AgentRuntime_Should_Execute_AssistantAgent_Successfully()
    {
        // Arrange
        var mockProvider = new MockLanguageModelProvider("Gemini", "Procesado por Gemini");
        var selector = new SingleProviderSelector(mockProvider);
        var agent = new EnterpriseAssistantAgent(selector, NullLogger<EnterpriseAssistantAgent>.Instance);
        var runtime = new AgentRuntime(agent, NullLogger<AgentRuntime>.Instance);

        var context = new AgentContext(
            EnterpriseAssistantAgent.EnterpriseAssistantAgentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Hola OCAP");

        // Act
        var output = await runtime.ExecuteAgentAsync(context);

        // Assert
        output.Should().Be("Procesado por Gemini");
    }

    [Fact]
    public async Task AgentResolver_Should_Resolve_To_EnterpriseAssistantAgent()
    {
        // Arrange
        var resolver = new AgentResolver();

        // Act
        var agentId = await resolver.ResolveAgentIdAsync(Guid.NewGuid(), Guid.NewGuid(), "Cualquier consulta");

        // Assert
        agentId.Should().Be(EnterpriseAssistantAgent.EnterpriseAssistantAgentId);
    }

    [Fact]
    public async Task ProcessAgentMessageUseCase_Should_Delegate_To_AgentRuntime_When_Present()
    {
        // Arrange
        var mockProvider = new MockLanguageModelProvider("Ollama", "Procesado localmente con Ollama");
        var selector = new SingleProviderSelector(mockProvider);
        var agent = new EnterpriseAssistantAgent(selector, NullLogger<EnterpriseAssistantAgent>.Instance);
        var runtime = new AgentRuntime(agent, NullLogger<AgentRuntime>.Instance);
        var resolver = new AgentResolver();

        var agentRepoMock = new Mock<IAgentRepository>();
        var contextRepoMock = new Mock<IConversationContextRepository>();
        var actionDispatcherMock = new Mock<IActionDispatcher>();

        var useCase = new ProcessAgentMessageUseCase(
            agentRepoMock.Object,
            contextRepoMock.Object,
            new RuleBasedIntentResolver(),
            actionDispatcherMock.Object,
            NullLogger<ProcessAgentMessageUseCase>.Instance,
            resolver,
            runtime);

        // Act
        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "Analizar reporte", Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().Be("Procesado localmente con Ollama");
    }

    [Fact]
    public void Agents_Abstractions_Should_Not_Depend_On_Channels_Or_Infrastructure()
    {
        // Hexagonal Boundary Verification
        var abstractionsAssembly = typeof(IAgentRuntime).Assembly;
        var referencedAssemblies = abstractionsAssembly.GetReferencedAssemblies();

        referencedAssemblies.Should().NotContain(a => a.Name!.StartsWith("OCAP.Channels"), "Agents.Abstractions must not reference Channels");
        referencedAssemblies.Should().NotContain(a => a.Name!.StartsWith("OCAP.Infrastructure"), "Agents.Abstractions must not reference Infrastructure");
    }
}

public class MockLanguageModelProvider : ILanguageModelProvider
{
    public string ProviderName { get; }
    private readonly string _response;

    public MockLanguageModelProvider(string providerName, string response)
    {
        ProviderName = providerName;
        _response = response;
    }

    public Task<LanguageModelResponse> GenerateAsync(LanguageModelRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LanguageModelResponse(_response, ProviderName, "mock-v1", 42));
    }
}

public class SingleProviderSelector : ILanguageModelProviderSelector
{
    private readonly ILanguageModelProvider _provider;

    public SingleProviderSelector(ILanguageModelProvider provider)
    {
        _provider = provider;
    }

    public Task<ILanguageModelProvider> GetProviderAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_provider);
    }
}
