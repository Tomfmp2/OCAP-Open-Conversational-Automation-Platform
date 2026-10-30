using FluentAssertions;
using OCAP.Channels.WhatsApp.Configuration;

namespace OCAP.Channels.WhatsApp.Tests;

public class WhatsAppSettingsTests
{
    [Fact]
    public void WebhookSecret_IsOptional_AndDerivedFromTheApiKey()
    {
        var settings = new WhatsAppSettings { ApiKey = "clave-evolution" };

        settings.EffectiveWebhookSecret.Should().NotBeNullOrWhiteSpace().And.NotBe("clave-evolution");
    }

    [Fact]
    public void ExplicitWebhookSecret_TakesPrecedence()
    {
        var settings = new WhatsAppSettings { ApiKey = "clave-evolution", WebhookSecret = "secreto-propio" };

        settings.EffectiveWebhookSecret.Should().Be("secreto-propio");
    }

    [Fact]
    public void WithoutApiKeyOrSecret_ThereIsNoKey()
    {
        new WhatsAppSettings().EffectiveWebhookSecret.Should().BeEmpty();
    }
}
