using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;
using OCAP.Intelligence.Application.Services;
using OCAP.Prompts;
using OCAP.Providers.Google.Calendar;
using OCAP.Tools.Abstractions;
using OCAP.Tools.Google;

namespace OCAP.Intelligence.Tests;

public class AgentReasoningServiceTests
{
    [Fact]
    public async Task ProcessReasoning_WithMeetingRequest_ExecutesToolAndReturnsResponse()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var config = new AgentConfiguration("Eres el asistente de reuniones de OCAP.");
        var agent = new Agent(agentId, new AgentName("AsistenteReuniones"), "Agente IA", config);

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var mockToolRegistry = new Mock<IToolRegistry>();
        var calendarProvider = new InMemoryCalendarProvider();
        var tool = new CreateCalendarEventTool(calendarProvider);
        mockToolRegistry.Setup(r => r.GetAllTools())
            .Returns(new List<ITool> { tool });

        var promptBuilder = new SystemPromptBuilder();
        var aiProvider = new Mock<OCAP.Intelligence.Abstractions.IAiProvider>();
        aiProvider.Setup(p => p.AnalyzeIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new Intent(Intent.CreateReminder, 0.95));
        aiProvider.Setup(p => p.GenerateResponseAsync(It.IsAny<OCAP.Intelligence.Abstractions.AiRequest>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new OCAP.Intelligence.Abstractions.AiResponse { ProviderName = "Mock", GeneratedText = "Acción Ejecutada con éxito: Evento de prueba agendado exitosamente en Google Calendar." });

        var mockDispatcher = new Mock<IActionDispatcher>();
        mockDispatcher.Setup(d => d.DispatchActionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AgentAction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResult.Ok(message: "Evento de prueba agendado exitosamente en Google Calendar."));

        var usageTracker = new AiUsageTracker(NullLogger<AiUsageTracker>.Instance);
        var logger = NullLogger<AgentReasoningService>.Instance;

        var service = new AgentReasoningService(
            mockAgentRepo.Object,
            mockToolRegistry.Object,
            promptBuilder,
            aiProvider.Object,
            mockDispatcher.Object,
            usageTracker,
            logger
        );

        // Act
        var result = await service.ProcessReasoningAsync(agentId, userId, conversationId, "Agendar una reunión mañana a las 3pm");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Acción Ejecutada");
        result.Should().Contain("Google Calendar");
    }
}
