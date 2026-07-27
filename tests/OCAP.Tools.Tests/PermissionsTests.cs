using FluentAssertions;
using OCAP.Providers.Google.Calendar;
using OCAP.Security.Abstractions;
using OCAP.Tools.Google;

namespace OCAP.Tools.Tests;

public class PermissionsTests
{
    [Fact]
    public async Task CanExecuteToolAsync_WhenPermissionAllowed_ReturnsTrue()
    {
        // Arrange
        var validator = new DefaultPermissionValidator();
        var agentId = Guid.NewGuid();
        var policy = new AgentPermissionPolicy(agentId);
        policy.Allow("Calendar.Create");
        validator.SetPolicy(policy);

        var tool = new CreateCalendarEventTool(new InMemoryCalendarProvider());

        // Act
        var canExecute = await validator.CanExecuteToolAsync(agentId, tool);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public async Task CanExecuteToolAsync_WhenPermissionDenied_ReturnsFalse()
    {
        // Arrange
        var validator = new DefaultPermissionValidator();
        var agentId = Guid.NewGuid();
        var policy = new AgentPermissionPolicy(agentId);
        policy.Deny("Calendar.Create");
        validator.SetPolicy(policy);

        var tool = new CreateCalendarEventTool(new InMemoryCalendarProvider());

        // Act
        var canExecute = await validator.CanExecuteToolAsync(agentId, tool);

        // Assert
        canExecute.Should().BeFalse();
    }
}
