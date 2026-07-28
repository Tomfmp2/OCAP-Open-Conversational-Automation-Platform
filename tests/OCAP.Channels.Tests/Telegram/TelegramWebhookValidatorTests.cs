using FluentAssertions;
using Microsoft.Extensions.Options;
using OCAP.Channels.Telegram.Configuration;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Tests.Telegram;

public class TelegramWebhookValidatorTests
{
    [Fact]
    public void ValidateSecret_WhenSecretMatchesOptions_ReturnsTrue()
    {
        // Arrange
        var options = Options.Create(new TelegramOptions
        {
            SecretToken = "super_secret_token_123",
            EnableWebhookValidation = true
        });
        var validator = new TelegramWebhookValidator(options);

        // Act
        var result = validator.ValidateSecret("super_secret_token_123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSecret_WhenSecretMismatch_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new TelegramOptions
        {
            SecretToken = "super_secret_token_123",
            EnableWebhookValidation = true
        });
        var validator = new TelegramWebhookValidator(options);

        // Act
        var result = validator.ValidateSecret("invalid_secret_token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePayload_WithValidUpdate_ReturnsTrue()
    {
        // Arrange
        var options = Options.Create(new TelegramOptions());
        var validator = new TelegramWebhookValidator(options);
        var update = new TelegramUpdate
        {
            UpdateId = 100,
            Message = new TelegramMessage
            {
                MessageId = 1,
                Chat = new TelegramChat { Id = 839292929, Type = "private" },
                Text = "Hola OCAP"
            }
        };

        // Act
        var result = validator.ValidatePayload(update);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePayload_WithNullOrEmptyText_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new TelegramOptions());
        var validator = new TelegramWebhookValidator(options);
        var update = new TelegramUpdate
        {
            UpdateId = 101,
            Message = new TelegramMessage
            {
                MessageId = 2,
                Chat = new TelegramChat { Id = 839292929, Type = "private" },
                Text = "   "
            }
        };

        // Act
        var result = validator.ValidatePayload(update);

        // Assert
        result.Should().BeFalse();
    }
}
