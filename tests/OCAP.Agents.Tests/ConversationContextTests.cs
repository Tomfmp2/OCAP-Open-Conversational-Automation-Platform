using FluentAssertions;
using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Tests;

// Pruebas unitarias para ConversationContext.
public class ConversationContextTests
{
    [Fact]
    public void SetIntentAndParameters_UpdatesStateCorrectly()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var context = new ConversationContext(conversationId);

        // Act
        context.SetIntent("CreateReminder");
        context.SetParameter("Date", "Tomorrow");
        context.UpdateState("Step", 1);

        // Assert
        context.ConversationId.Should().Be(conversationId);
        context.CurrentIntent.Should().Be("CreateReminder");
        context.PendingParameters["Date"].Should().Be("Tomorrow");
        context.State["Step"].Should().Be(1);
    }

    [Fact]
    public void ClearIntent_ResetsIntentAndPendingParameters()
    {
        // Arrange
        var context = new ConversationContext(Guid.NewGuid());
        context.SetIntent("Greeting");
        context.SetParameter("Name", "Pedro");

        // Act
        context.ClearIntent();

        // Assert
        context.CurrentIntent.Should().BeNull();
        context.PendingParameters.Should().BeEmpty();
    }
}
