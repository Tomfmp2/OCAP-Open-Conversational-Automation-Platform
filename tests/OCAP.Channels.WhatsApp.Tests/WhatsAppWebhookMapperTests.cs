using FluentAssertions;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Channels.WhatsApp.Tests;

// Pruebas unitarias para WhatsAppWebhookMapper.
// Verifica la transformación del payload de Evolution API a IncomingChannelMessage.
public class WhatsAppWebhookMapperTests
{
    [Fact]
    public void ToIncomingMessage_WithValidPayload_MapsCorrectly()
    {
        // Arrange
        var payload = new WhatsAppWebhookPayload
        {
            Instance = "ocap-main",
            Data = new WhatsAppWebhookData
            {
                PushName = "Juan Pérez",
                MessageTimestamp = 1700000000,
                Key = new WhatsAppMessageKey
                {
                    RemoteJid = "573001234567@s.whatsapp.net",
                    Id = "MSG_123456"
                },
                Message = new WhatsAppMessageBody
                {
                    Conversation = "  Hola bot  "
                }
            }
        };

        // Act
        var message = WhatsAppWebhookMapper.ToIncomingMessage(payload);

        // Assert
        message.ChannelName.Should().Be("WhatsApp");
        message.ExternalUserId.Should().Be("573001234567");
        message.Message.Should().Be("Hola bot");
        message.Metadata["PushName"].Should().Be("Juan Pérez");
        message.Metadata["MessageId"].Should().Be("MSG_123456");
        message.Metadata["Instance"].Should().Be("ocap-main");
    }
}
