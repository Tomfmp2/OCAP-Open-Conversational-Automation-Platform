using FluentAssertions;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;

namespace OCAP.Agents.Tests;

// Pruebas unitarias para la entidad y Aggregate Root de Agent.
public class AgentEntityTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesActiveAgent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = new AgentName("Asistente Principal");
        var config = new AgentConfiguration("Prompt de prueba");

        // Act
        var agent = new Agent(id, name, "Descripción de prueba", config);

        // Assert
        agent.Id.Should().Be(id);
        agent.Name.Value.Should().Be("Asistente Principal");
        agent.Status.Should().Be(AgentStatus.Active);
        agent.Configuration.SystemPrompt.Should().Be("Prompt de prueba");
    }

    [Fact]
    public void AgentName_WithEmptyValue_ThrowsArgumentException()
    {
        // Act
        Action act = () => new AgentName("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StatusTransitions_UpdateStatusAndTimestampCorrectly()
    {
        // Arrange
        var agent = new Agent(Guid.NewGuid(), new AgentName("Agente 1"), "Desc", new AgentConfiguration("Prompt"));

        // Act & Assert - Deactivate
        agent.Deactivate();
        agent.Status.Should().Be(AgentStatus.Inactive);
        agent.UpdatedAt.Should().NotBeNull();

        // Act & Assert - Maintenance
        agent.SetMaintenance();
        agent.Status.Should().Be(AgentStatus.Maintenance);

        // Act & Assert - Activate
        agent.Activate();
        agent.Status.Should().Be(AgentStatus.Active);
    }
}
