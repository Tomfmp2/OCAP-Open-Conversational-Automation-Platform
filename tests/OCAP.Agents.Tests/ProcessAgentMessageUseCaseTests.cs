using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Application.UseCases;
using OCAP.Agents.Domain.Entities;
using OCAP.Security.Abstractions;

namespace OCAP.Agents.Tests;

// Pruebas de integración del pipeline completo de Agent Engine a través de ProcessAgentMessageUseCase.
public class ProcessAgentMessageUseCaseTests
{
    private readonly Mock<IAgentRepository> _agentRepoMock;
    private readonly Mock<IConversationContextRepository> _contextRepoMock;
    private readonly ProcessAgentMessageUseCase _useCase;

    public ProcessAgentMessageUseCaseTests()
    {
        _agentRepoMock = new Mock<IAgentRepository>();
        _contextRepoMock = new Mock<IConversationContextRepository>();

        var resolver = new RuleBasedIntentResolver();
        var toolRegistry = new ToolRegistry();
        var permissionValidator = new DefaultPermissionValidator();
        var dispatcher = new ActionDispatcher(toolRegistry, permissionValidator, NullLogger<ActionDispatcher>.Instance);

        _useCase = new ProcessAgentMessageUseCase(
            _agentRepoMock.Object,
            _contextRepoMock.Object,
            resolver,
            dispatcher,
            NullLogger<ProcessAgentMessageUseCase>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_GreetingMessage_ReturnsGreetingResponse()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        var response = await _useCase.ExecuteAsync(conversationId, "Hola buenos días");

        // Assert
        response.Should().Contain("Hola");
    }

    [Fact]
    public async Task ExecuteAsync_ReminderMessage_ReturnsReminderResponse()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        var response = await _useCase.ExecuteAsync(conversationId, "Por favor agendar una reunión mañana");

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
    }
}
