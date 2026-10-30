using FluentAssertions;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Channels.WhatsApp.Tests;

// Pruebas unitarias para WhatsAppWebhookMapper.
// Verifica la transformación del payload de WhatsApp Cloud API a IncomingChannelMessage.
public class WhatsAppWebhookMapperTests
{
    [Fact]
    public void ToIncomingMessage_WithValidPayload_MapsCorrectly()
    {
        // Arrange
        var payload = new WhatsAppCloudWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry = new List<WhatsAppWebhookEntry>
            {
                new WhatsAppWebhookEntry
                {
                    Id = "123456",
                    Changes = new List<WhatsAppWebhookChange>
                    {
                        new WhatsAppWebhookChange
                        {
                            Field = "messages",
                            Value = new WhatsAppWebhookValue
                            {
                                MessagingProduct = "whatsapp",
                                Metadata = new WhatsAppWebhookMetadata
                                {
                                    DisplayPhoneNumber = "1234567890",
                                    PhoneNumberId = "0987654321"
                                },
                                Contacts = new List<WhatsAppWebhookContact>
                                {
                                    new WhatsAppWebhookContact
                                    {
                                        Profile = new WhatsAppWebhookProfile { Name = "Juan Pérez" },
                                        WaId = "573001234567"
                                    }
                                },
                                Messages = new List<WhatsAppWebhookMessage>
                                {
                                    new WhatsAppWebhookMessage
                                    {
                                        From = "573001234567",
                                        Id = "MSG_123456",
                                        Timestamp = "1700000000",
                                        Type = "text",
                                        Text = new WhatsAppWebhookText { Body = "Hola bot" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var message = WhatsAppWebhookMapper.ToIncomingMessage(payload);

        // Assert
        message.Should().NotBeNull();
        message!.ChannelName.Should().Be("WhatsApp");
        message.ExternalUserId.Should().Be("573001234567");
        message.Message.Should().Be("Hola bot");
        message.Metadata["SenderName"].Should().Be("Juan Pérez");
        message.Metadata["WaId"].Should().Be("573001234567");
        message.Metadata["MessageId"].Should().Be("MSG_123456");
        message.Metadata["MessageType"].Should().Be("text");
        message.Metadata["PhoneNumberId"].Should().Be("0987654321");
        message.Metadata["DisplayPhoneNumber"].Should().Be("1234567890");
    }

    [Fact]
    public void ToIncomingMessage_WithoutMessages_ReturnsNull()
    {
        // Arrange
        var payload = new WhatsAppCloudWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry = new List<WhatsAppWebhookEntry>
            {
                new WhatsAppWebhookEntry
                {
                    Id = "123456",
                    Changes = new List<WhatsAppWebhookChange>
                    {
                        new WhatsAppWebhookChange
                        {
                            Field = "messages",
                            Value = new WhatsAppWebhookValue
                            {
                                MessagingProduct = "whatsapp"
                                // Messages is null (e.g. status update)
                            }
                        }
                    }
                }
            }
        };

        // Act
        var message = WhatsAppWebhookMapper.ToIncomingMessage(payload);

        // Assert
        message.Should().BeNull();
    }
}
