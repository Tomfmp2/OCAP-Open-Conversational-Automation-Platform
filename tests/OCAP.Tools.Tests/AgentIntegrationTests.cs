using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Application.UseCases;
using OCAP.Agents.Domain.Entities;
using OCAP.Providers.Google.Calendar;
using OCAP.Security.Abstractions;
using OCAP.Tools.Google;

namespace OCAP.Tools.Tests;

public class AgentIntegrationTests
{
    [Fact]
    public async Task ProcessAgentMessage_WithCreateReminderMessage_ExecutesCalendarToolSuccessfully()
    {
        // Arrange
        var agentRepoMock = new Mock<IAgentRepository>();
        var contextRepoMock = new Mock<IConversationContextRepository>();

        var calendarProvider = new InMemoryCalendarProvider();
        var tool = new CreateCalendarEventTool(calendarProvider);

        var toolRegistry = new ToolRegistryImpl();
        toolRegistry.RegisterTool(tool);

        var permissionValidator = new DefaultPermissionValidator();

        var actionDispatcher = new ActionDispatcher(toolRegistry, permissionValidator, NullLogger<ActionDispatcher>.Instance);
        var intentResolver = new RuleBasedIntentResolver();

        var useCase = new ProcessAgentMessageUseCase(
            agentRepoMock.Object,
            contextRepoMock.Object,
            intentResolver,
            actionDispatcher,
            NullLogger<ProcessAgentMessageUseCase>.Instance);

        var conversationId = Guid.NewGuid();

        // Act
        var response = await useCase.ExecuteAsync(conversationId, "Por favor agendar una reunión mañana");

        // Assert
        response.Should().Contain("Recordatorio registrado");

        var events = await calendarProvider.GetEventsAsync(DateTime.MinValue, DateTime.MaxValue);
        events.Should().HaveCount(1);
        events.First().Title.Should().Be("Recordatorio solicitado por usuario");
    }
}
