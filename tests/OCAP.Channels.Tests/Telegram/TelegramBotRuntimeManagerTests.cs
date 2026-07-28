using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Channels.Telegram.Configuration;
using OCAP.Channels.Telegram.Services;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Services;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Channels.Tests.Telegram;

public class TelegramBotRuntimeManagerTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ResponseToReturn);
        }
    }

    private OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsBotInfo()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""ok"":true,""result"":{""id"":123456789,""is_bot"":true,""first_name"":""OCAP_Bot"",""username"":""ocap_test_bot""}}")
            }
        };

        var httpClient = new HttpClient(handler);
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramOptions { BotToken = "123:ABC" });
        var apiClient = new TelegramApiClient(httpClient, options, NullLogger<TelegramApiClient>.Instance);

        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance);
        var registry = new ChannelRegistry();
        var connectionManager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);
        var auditService = new SecurityAuditService(NullLogger<SecurityAuditService>.Instance);

        var manager = new TelegramBotRuntimeManager(
            apiClient,
            connectionManager,
            auditService,
            NullLogger<TelegramBotRuntimeManager>.Instance);

        // Act
        var result = await manager.ValidateTokenAsync("123:ABC");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123456789, result.Id);
        Assert.True(result.IsBot);
        Assert.Equal("ocap_test_bot", result.Username);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(@"{""ok"":false,""error_code"":401,""description"":""Unauthorized""}")
            }
        };

        var httpClient = new HttpClient(handler);
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramOptions { BotToken = "invalid" });
        var apiClient = new TelegramApiClient(httpClient, options, NullLogger<TelegramApiClient>.Instance);

        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance);
        var registry = new ChannelRegistry();
        var connectionManager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);
        var auditService = new SecurityAuditService(NullLogger<SecurityAuditService>.Instance);

        var manager = new TelegramBotRuntimeManager(
            apiClient,
            connectionManager,
            auditService,
            NullLogger<TelegramBotRuntimeManager>.Instance);

        // Act
        var result = await manager.ValidateTokenAsync("invalid");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HealthCheckAsync_WithValidToken_ReturnsHealthyStatus()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""ok"":true,""result"":{""id"":999,""is_bot"":true,""first_name"":""Test"",""username"":""health_bot""}}")
            }
        };

        var httpClient = new HttpClient(handler);
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramOptions());
        var apiClient = new TelegramApiClient(httpClient, options, NullLogger<TelegramApiClient>.Instance);

        using var dbContext = CreateDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance);
        var registry = new ChannelRegistry();
        var connectionManager = new ChannelConnectionManager(dbContext, vault, registry, NullLogger<ChannelConnectionManager>.Instance);
        var auditService = new SecurityAuditService(NullLogger<SecurityAuditService>.Instance);

        var manager = new TelegramBotRuntimeManager(
            apiClient,
            connectionManager,
            auditService,
            NullLogger<TelegramBotRuntimeManager>.Instance);

        // Act
        var result = await manager.HealthCheckAsync("999:TEST");

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("health_bot", result.BotUsername);
        Assert.True(result.LatencyMs >= 0);
    }
}
