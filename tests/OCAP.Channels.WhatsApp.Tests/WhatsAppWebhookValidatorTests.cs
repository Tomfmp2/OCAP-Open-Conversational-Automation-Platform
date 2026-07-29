using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.DTOs;
using OCAP.Channels.WhatsApp.Webhooks;

namespace OCAP.Channels.WhatsApp.Tests;

// Pruebas unitarias para WhatsAppWebhookValidator.
// Verifica la validación de seguridad de payloads y firmas HMAC de WhatsApp Cloud API.
public class WhatsAppWebhookValidatorTests
{
    private readonly WhatsAppWebhookValidator _validator;
    private readonly string _appSecret = "my_app_secret";
    private readonly string _verifyToken = "my_verify_token";

    public WhatsAppWebhookValidatorTests()
    {
        var settings = Options.Create(new WhatsAppSettings
        {
            AppSecret = _appSecret,
            WebhookVerifyToken = _verifyToken
        });
        _validator = new WhatsAppWebhookValidator(settings, NullLogger<WhatsAppWebhookValidator>.Instance);
    }

    [Fact]
    public void ValidateVerifyToken_WithCorrectTokens_ReturnsTrue()
    {
        // Act
        var result = _validator.ValidateVerifyToken("subscribe", _verifyToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateVerifyToken_WithWrongMode_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateVerifyToken("unsubscribe", _verifyToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateVerifyToken_WithWrongToken_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateVerifyToken("subscribe", "wrong_token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var payload = "{\"object\":\"whatsapp_business_account\"}";
        var expectedHash = GenerateHmacSha256(payload, _appSecret);
        var signatureHeader = $"sha256={expectedHash}";

        // Act
        var result = _validator.ValidateSignature(payload, signatureHeader);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignature_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"object\":\"whatsapp_business_account\"}";
        var expectedHash = GenerateHmacSha256(payload, "wrong_secret");
        var signatureHeader = $"sha256={expectedHash}";

        // Act
        var result = _validator.ValidateSignature(payload, signatureHeader);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePayload_WithValidPayload_ReturnsTrue()
    {
        // Arrange
        var payload = new WhatsAppCloudWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry = new List<WhatsAppCloudEntry>
            {
                new WhatsAppCloudEntry
                {
                    Changes = new List<WhatsAppCloudChange>
                    {
                        new WhatsAppCloudChange
                        {
                            Field = "messages"
                        }
                    }
                }
            }
        };

        // Act
        var result = _validator.ValidatePayload(payload);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePayload_WithInvalidObject_ReturnsFalse()
    {
        // Arrange
        var payload = new WhatsAppCloudWebhookPayload
        {
            Object = "page", // Incorrect
            Entry = new List<WhatsAppCloudEntry>()
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
        var result = _validator.ValidatePayload(null!);

        // Assert
        result.Should().BeFalse();
    }

    private string GenerateHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
