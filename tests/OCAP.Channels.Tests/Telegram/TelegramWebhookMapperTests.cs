using FluentAssertions;
using OCAP.Channels.Abstractions.Models;
using OCAP.Channels.Telegram.DTOs;
using OCAP.Channels.Telegram.Webhooks;

namespace OCAP.Channels.Tests.Telegram;

public class TelegramWebhookMapperTests
{
    [Fact]
    public void ToIncomingMessage_MapsTelegramUpdateToIncomingChannelMessageCorrectly()
    {
        // Arrange
        var update = new TelegramUpdate
        {
            UpdateId = 12345,
            Message = new TelegramMessage
            {
                MessageId = 999,
                Chat = new TelegramChat
                {
                    Id = 839292929,
                    Type = "private"
                },
                From = new TelegramUser
                {
                    Id = 839292929,
                    FirstName = "Carlos",
                    LastName = "Dev",
                    Username = "carlosdev"
                },
                Text = "Hola desde Telegram"
            }
        };

        // Act
        var incomingMessage = TelegramWebhookMapper.ToIncomingMessage(update);

        // Assert
        incomingMessage.Should().NotBeNull();
        incomingMessage.ExternalUserId.Should().Be("839292929");
        incomingMessage.Message.Should().Be("Hola desde Telegram");
        incomingMessage.ChannelName.Should().Be("Telegram");
        incomingMessage.Metadata["update_id"].Should().Be("12345");
        incomingMessage.Metadata["first_name"].Should().Be("Carlos");
        incomingMessage.Metadata["username"].Should().Be("carlosdev");
    }

    [Fact]
    public void ToSendMessageRequest_MapsOutgoingChannelMessageToTelegramRequestCorrectly()
    {
        // Arrange
        var outgoingMessage = new OutgoingChannelMessage
        {
            DestinationUserId = "839292929",
            Message = "Respuesta del Enterprise Assistant Agent",
            ChannelName = "Telegram"
        };

        // Act
        var request = TelegramWebhookMapper.ToSendMessageRequest(outgoingMessage);

        // Assert
        request.Should().NotBeNull();
        request.ChatId.Should().Be("839292929");
        request.Text.Should().Be("Respuesta del Enterprise Assistant Agent");
        request.ParseMode.Should().Be("Markdown");
    }
}
