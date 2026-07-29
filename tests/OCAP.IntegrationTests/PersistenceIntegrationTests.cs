using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Core.ValueObjects;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Application.Services;
using OCAP.Security.Abstractions;
using OCAP.Tools.Abstractions;
using OCAP.Core.Ports;
using OCAP.Intelligence.Domain;
using OCAP.Intelligence.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;

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

    [Fact]
    public async Task AgentRuntime_Persists_Conversations_Messages_Logs_And_Memory_To_Db()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var mockProvider = new MockLanguageModelProvider("Gemini", "Respuesta final del agente");
        var selector = new SingleProviderSelector(mockProvider);
        var assistantAgent = new EnterpriseAssistantAgent(selector, NullLogger<EnterpriseAssistantAgent>.Instance);
        
        var conversationRepo = new ConversationRepository(_context);
        var messageRepo = new MessageRepository(_context);
        var memoryRepo = new AiConversationMemoryRepository(_context);
        var executionLogRepo = new AiExecutionLogRepository(_context);

        var runtime = new AgentRuntime(
            assistantAgent,
            NullLogger<AgentRuntime>.Instance,
            null, // eventBus
            conversationRepo,
            messageRepo,
            memoryRepo,
            executionLogRepo
        );

        var envVars = new Dictionary<string, object> { ["ConversationId"] = conversationId };
        var context = new AgentContext(agentId, tenantId, userId, "Mensaje del usuario", envVars);

        // Act
        var response = await runtime.ExecuteAgentAsync(context);

        // Assert
        response.Should().Be("Respuesta final del agente");

        var dbConversation = await _context.Conversations.FindAsync(conversationId);
        Assert.NotNull(dbConversation);
        Assert.Equal(userId, dbConversation.UserId);

        var dbMessages = await _context.Messages.Where(m => m.ConversationId == conversationId).ToListAsync();
        Assert.Equal(2, dbMessages.Count);
        Assert.Contains(dbMessages, m => m.Content.Value == "Mensaje del usuario" && m.SenderType == SenderType.User);
        Assert.Contains(dbMessages, m => m.Content.Value == "Respuesta final del agente" && m.SenderType == SenderType.Agent);

        var dbLogs = await _context.AiExecutionLogs.ToListAsync();
        Assert.Single(dbLogs);
        Assert.Equal("Gemini", dbLogs.First().Provider);
        Assert.True(dbLogs.First().Success);

        var dbMemory = await _context.AiConversationMemories.Where(m => m.ConversationId == conversationId).ToListAsync();
        Assert.Equal(2, dbMemory.Count);
        Assert.Contains(dbMemory, m => m.Content.Contains("User: Mensaje del usuario"));
        Assert.Contains(dbMemory, m => m.Content.Contains("Agent: Respuesta final del agente"));
    }

    [Fact]
    public async Task ActionDispatcher_Persists_ToolExecutions_To_Db()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var toolRegistryMock = new Mock<IToolRegistry>();
        var toolMock = new Mock<ITool>();
        toolMock.Setup(t => t.Definition).Returns(new ToolDefinition { Name = "MockTool" });
        toolMock.Setup(t => t.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResult.Ok(message: "Resultado del MockTool"));

        toolRegistryMock.Setup(r => r.GetTool("MockTool")).Returns(toolMock.Object);

        var permissionValidatorMock = new Mock<IPermissionValidator>();
        permissionValidatorMock.Setup(p => p.CanExecuteToolAsync(agentId, toolMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var toolExecutionRepo = new ToolExecutionRepository(_context);
        var dispatcher = new ActionDispatcher(
            toolRegistryMock.Object,
            permissionValidatorMock.Object,
            NullLogger<ActionDispatcher>.Instance,
            toolExecutionRepo
        );

        var action = new AgentAction("CallTool", "MockTool", new Dictionary<string, object>());

        // Act
        var result = await dispatcher.DispatchActionAsync(agentId, userId, conversationId, action);

        // Assert
        Assert.True(result.Success);
        
        var dbExecutions = await _context.ToolExecutions.ToListAsync();
        Assert.Single(dbExecutions);
        Assert.Equal("MockTool", dbExecutions.First().ToolName);
        Assert.Equal(agentId, dbExecutions.First().AgentId);
        Assert.Equal(userId, dbExecutions.First().UserId);
        Assert.Equal(conversationId, dbExecutions.First().ConversationId);
        Assert.True(dbExecutions.First().Success);
    }
}

public class MockLanguageModelProvider : ILanguageModelProvider
{
    public string ProviderName { get; }
    private readonly string _response;

    public MockLanguageModelProvider(string providerName, string response)
    {
        ProviderName = providerName;
        _response = response;
    }

    public Task<LanguageModelResponse> GenerateAsync(LanguageModelRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LanguageModelResponse(_response, ProviderName, "mock-v1", 42));
    }
}

public class SingleProviderSelector : ILanguageModelProviderSelector
{
    private readonly ILanguageModelProvider _provider;

    public SingleProviderSelector(ILanguageModelProvider provider)
    {
        _provider = provider;
    }

    public Task<ILanguageModelProvider> GetProviderAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_provider);
    }
}
