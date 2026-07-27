using Moq;
using OCAP.Application.UseCases;
using OCAP.Core.Entities;
using OCAP.Core.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OCAP.UnitTests.Application;

public class ReceiveMessageUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly ReceiveMessageUseCase _useCase;

    public ReceiveMessageUseCaseTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _messageRepositoryMock = new Mock<IMessageRepository>();

        _useCase = new ReceiveMessageUseCase(
            _userRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            _messageRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _useCase.ExecuteAsync(userId, "Hello", "WhatsApp"));
    }

    [Fact]
    public async Task ExecuteAsync_ValidMessage_CreatesConversationAndSavesMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "TestUser");
        _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _conversationRepositoryMock.Setup(repo => repo.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation)null!);

        // Act
        await _useCase.ExecuteAsync(userId, "Hello", "WhatsApp");

        // Assert
        _conversationRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
