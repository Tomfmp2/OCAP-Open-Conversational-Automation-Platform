using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Channels.WhatsApp.Tests;

// Pruebas unitarias para WhatsAppWebhookValidator.
// Verifica la validación de seguridad de payloads, banderas fromMe, limites de tamaño y secrectos de webhook.
public class WhatsAppWebhookValidatorTests
{
    private readonly WhatsAppWebhookValidator _validator;

    public WhatsAppWebhookValidatorTests()
    {
        var settings = Options.Create(new WhatsAppSettings
        {
            WebhookSecret = "my_test_secret"
        });
        _validator = new WhatsAppWebhookValidator(settings, NullLogger<WhatsAppWebhookValidator>.Instance);
    }

    [Fact]
    public void ValidatePayload_WithValidPayload_ReturnsTrue()
    {
        // Arrange
        var payload = new WhatsAppWebhookPayload
        {
            Data = new WhatsAppWebhookData
            {
                Key = new WhatsAppMessageKey
                {
                    RemoteJid = "573001234567@s.whatsapp.net",
                    FromMe = false
                },
                Message = new WhatsAppMessageBody
                {
                    Conversation = "Hola OCAP WhatsApp"
                }
            }
        };

        // Act
        var result = _validator.ValidatePayload(payload);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePayload_WhenFromMeIsTrue_ReturnsFalse()
    {
        // Arrange: mensaje enviado por la propia instancia, debe ser ignorado.
        var payload = new WhatsAppWebhookPayload
        {
            Data = new WhatsAppWebhookData
            {
                Key = new WhatsAppMessageKey
                {
                    RemoteJid = "573001234567@s.whatsapp.net",
                    FromMe = true
                },
                Message = new WhatsAppMessageBody
                {
                    Conversation = "Mensaje enviado por mi"
                }
            }
        };

        // Act
        var result = _validator.ValidatePayload(payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePayload_WithNullPayload_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidatePayload(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePayload_ExceedingSizeLimit_ReturnsFalse()
    {
        // Arrange: payload con más de 10 KB de texto.
        var hugeText = new string('X', 11 * 1024);
        var payload = new WhatsAppWebhookPayload
        {
            Data = new WhatsAppWebhookData
            {
                Key = new WhatsAppMessageKey { RemoteJid = "573001234567@s.whatsapp.net", FromMe = false },
                Message = new WhatsAppMessageBody { Conversation = hugeText }
            }
        };

        // Act
        var result = _validator.ValidatePayload(payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSecret_WithMatchingSecret_ReturnsTrue()
    {
        // Act
        var result = _validator.ValidateSecret("my_test_secret");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSecret_WithInvalidSecret_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateSecret("wrong_secret");

        // Assert
        result.Should().BeFalse();
    }
}
