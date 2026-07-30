using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Application.Services;
using OCAP.Agents.Application.UseCases;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;
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

        var agentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var agent = new Agent(agentId, new AgentName("Test Agent"), "test", new AgentConfiguration("sys"));
        agentRepoMock.Setup(r => r.GetDefaultAgentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var permissionValidator = new DefaultPermissionValidator();
        var policy = new AgentPermissionPolicy(agentId);
        policy.Allow("Calendar.Create");
        permissionValidator.SetPolicy(policy);

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
