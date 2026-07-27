using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Core.ValueObjects;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OCAP.IntegrationTests;

public class PersistenceIntegrationTests : IDisposable
{
    private readonly OCAPDbContext _context;
    private readonly UserRepository _userRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly MessageRepository _messageRepository;

    public PersistenceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new OCAPDbContext(options);
        
        _userRepository = new UserRepository(_context);
        _conversationRepository = new ConversationRepository(_context);
        _messageRepository = new MessageRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_User()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "Test User");

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var retrievedUser = await _userRepository.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal("Test User", retrievedUser.DisplayName);
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_Conversation_And_Messages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "Test User");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversationId = Guid.NewGuid();
        var conversation = new Conversation(conversationId, userId);
        
        var messageId = Guid.NewGuid();
        var content = new MessageContent("Hello OCAP");
        var message = new Message(messageId, conversationId, content, SenderType.User);

        // Act
        await _conversationRepository.SaveAsync(conversation);
        await _messageRepository.SaveAsync(message);

        var retrievedConversation = await _conversationRepository.GetByIdAsync(conversationId);
        var retrievedMessages = await _messageRepository.GetByConversationIdAsync(conversationId);

        // Assert
        Assert.NotNull(retrievedConversation);
        Assert.Equal(userId, retrievedConversation.UserId);
        
        Assert.Single(retrievedMessages);
        Assert.Equal("Hello OCAP", retrievedMessages.First().Content.Value);
    }
}
