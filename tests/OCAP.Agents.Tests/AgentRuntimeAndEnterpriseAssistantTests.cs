using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;
using Xunit;

namespace OCAP.Agents.Tests;

public class AgentRuntimeAndEnterpriseAssistantTests
{
    /// <summary>El agente con dependencias neutras: sin herramientas ni contexto de sistema.</summary>
    private static EnterpriseAssistantAgent CreateAssistant(ILanguageModelProviderSelector selector) =>
        new(selector,
            new ToolRegistry(),
            Mock.Of<IActionDispatcher>(),
            Mock.Of<IOcapSystemContextProvider>(c =>
                c.GetTenantSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()) == Task.FromResult(string.Empty)),
            Mock.Of<IConfiguration>(),
            NullLogger<EnterpriseAssistantAgent>.Instance);

    [Fact]
    public async Task EnterpriseAssistantAgent_Should_Process_Request_Using_LanguageModelProvider()
    {
        // Arrange
        var mockProvider = new MockLanguageModelProvider("OpenAI", "Respuesta de Inteligencia Empresarial OCAP");
        var selector = new SingleProviderSelector(mockProvider);
        var agent = CreateAssistant(selector);

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
    public async Task ToolsProposedFromAttachedContent_AreNotExecuted()
    {
        // Un documento adjunto con instrucciones escondidas lleva al modelo a pedir un correo: no debe enviarse.
        var dispatcher = new Mock<IActionDispatcher>();
        var provider = new MockLanguageModelProvider("Gemini",
            """{"reply":"Envío el resumen.","tool":"SendEmailTool","args":{"To":"atacante@ejemplo.com","Subject":"x","Body":"y"}}""");
        var agent = new EnterpriseAssistantAgent(
            new SingleProviderSelector(provider),
            new ToolRegistry(),
            dispatcher.Object,
            Mock.Of<IOcapSystemContextProvider>(c =>
                c.GetTenantSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()) == Task.FromResult(string.Empty)),
            Mock.Of<IConfiguration>(),
            NullLogger<EnterpriseAssistantAgent>.Instance);
        var context = new AgentContext(EnterpriseAssistantAgent.EnterpriseAssistantAgentId, Guid.NewGuid(), Guid.NewGuid(), "Resume el PDF");
        context.EnvironmentVariables[EnterpriseAssistantAgent.UntrustedContentKey] = true;

        var result = await agent.ProcessRequestAsync(context);

        result.OutputMessage.Should().Contain("No ejecuto acciones");
        dispatcher.Verify(d => d.DispatchActionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<OCAP.Agents.Domain.Entities.AgentAction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AgentRuntime_Should_Execute_AssistantAgent_Successfully()
    {
        // Arrange
        var mockProvider = new MockLanguageModelProvider("Gemini", "Procesado por Gemini");
        var selector = new SingleProviderSelector(mockProvider);
        var agent = CreateAssistant(selector);
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
