using OCAP.Core.Entities;
using OCAP.Core.Events;
using System;
using System.Linq;
using Xunit;

namespace OCAP.UnitTests.Core;

public class ConversationTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesActiveConversation()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var conversation = new Conversation(id, userId);

        // Assert
        Assert.Equal(id, conversation.Id);
        Assert.Equal(userId, conversation.UserId);
        Assert.Equal(ConversationStatus.Active, conversation.Status);
        
        var startEvent = conversation.DomainEvents.OfType<ConversationStartedEvent>().SingleOrDefault();
        Assert.NotNull(startEvent);
        Assert.Equal(id, startEvent.ConversationId);
    }

    [Fact]
    public void Close_WhenActive_ChangesStatusToClosedAndRegistersEvent()
    {
        // Arrange
        var conversation = new Conversation(Guid.NewGuid(), Guid.NewGuid());
        conversation.ClearDomainEvents();

        // Act
        conversation.Close();

        // Assert
        Assert.Equal(ConversationStatus.Closed, conversation.Status);
        var closeEvent = conversation.DomainEvents.OfType<ConversationClosedEvent>().SingleOrDefault();
        Assert.NotNull(closeEvent);
    }

    [Fact]
    public void RequestHumanIntervention_WhenActive_ChangesStatusToWaitingHuman()
    {
        // Arrange
        var conversation = new Conversation(Guid.NewGuid(), Guid.NewGuid());
        conversation.ClearDomainEvents();

        // Act
        conversation.RequestHumanIntervention();

        // Assert
        Assert.Equal(ConversationStatus.WaitingHuman, conversation.Status);
        var humanEvent = conversation.DomainEvents.OfType<HumanInterventionRequestedEvent>().SingleOrDefault();
        Assert.NotNull(humanEvent);
    }
}
