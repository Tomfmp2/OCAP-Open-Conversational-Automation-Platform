using FluentAssertions;
using OCAP.Channels.WhatsApp.Evolution;

namespace OCAP.Channels.WhatsApp.Tests.Evolution;

/// <summary>Payloads con la forma que envía Evolution API v2.3.7.</summary>
public class EvolutionWebhookParserTests
{
    private const string Instance = "ocap-00000000000000000000000000000001";

    private static string Message(string remoteJid, bool fromMe = false, string text = "Hola", string id = "3EB0A1", string? remoteJidAlt = null) => $$"""
        {
          "event": "messages.upsert",
          "instance": "{{Instance}}",
          "data": {
            "key": { "remoteJid": "{{remoteJid}}", "fromMe": {{(fromMe ? "true" : "false")}}, "id": "{{id}}"{{(remoteJidAlt is null ? "" : $", \"remoteJidAlt\": \"{remoteJidAlt}\", \"addressingMode\": \"lid\"")}} },
            "pushName": "Ana",
            "message": { "conversation": "{{text}}" },
            "messageType": "conversation",
            "messageTimestamp": 1761200000
          },
          "date_time": "2026-10-23T10:00:00.000Z",
          "apikey": "no-se-usa"
        }
        """;

    [Fact]
    public void TextMessageFromAContact_IsParsed()
    {
        var parsed = EvolutionWebhookParser.Parse(Message("573001234567@s.whatsapp.net"));

        parsed.Should().BeEquivalentTo(new EvolutionTextMessageEvent(Instance, "3EB0A1", "573001234567", "Ana", "Hola"));
    }

    [Fact]
    public void ContactIdentifiedByLid_IsAnsweredAtItsRealNumber()
    {
        // WhatsApp manda el LID en remoteJid y el número en remoteJidAlt (Evolution 2.3, addressingMode "lid").
        var parsed = EvolutionWebhookParser.Parse(Message("123456789012345@lid", remoteJidAlt: "573001234567@s.whatsapp.net"));

        parsed.Should().BeEquivalentTo(new EvolutionTextMessageEvent(Instance, "3EB0A1", "573001234567", "Ana", "Hola"));
    }

    [Fact]
    public void ContactWithOnlyALid_IsAnsweredAtTheLid()
    {
        var parsed = EvolutionWebhookParser.Parse(Message("123456789012345@lid"));

        parsed.Should().BeEquivalentTo(new EvolutionTextMessageEvent(Instance, "3EB0A1", "123456789012345@lid", "Ana", "Hola"));
    }

    [Fact]
    public void ExtendedTextMessage_IsParsed()
    {
        var json = $$"""
            { "event": "messages.upsert", "instance": "{{Instance}}",
              "data": { "key": { "remoteJid": "573001234567@s.whatsapp.net", "fromMe": false, "id": "X1" },
                        "message": { "extendedTextMessage": { "text": "con enlace https://ocap.io" } } } }
            """;

        var parsed = EvolutionWebhookParser.Parse(json).Should().BeOfType<EvolutionTextMessageEvent>().Subject;

        parsed.Text.Should().Be("con enlace https://ocap.io");
        parsed.SenderName.Should().BeEmpty();
    }

    [Theory]
    [InlineData("120363012345678901@g.us")]
    [InlineData("status@broadcast")]
    [InlineData("120363098765432109@newsletter")]
    public void MessagesOutsideIndividualChats_AreIgnored(string remoteJid)
    {
        EvolutionWebhookParser.Parse(Message(remoteJid)).Should().BeNull();
    }

    [Fact]
    public void OwnMessages_AreIgnored()
    {
        EvolutionWebhookParser.Parse(Message("573001234567@s.whatsapp.net", fromMe: true)).Should().BeNull();
    }

    [Fact]
    public void MessageWithoutText_IsIgnored()
    {
        var json = $$"""
            { "event": "messages.upsert", "instance": "{{Instance}}",
              "data": { "key": { "remoteJid": "573001234567@s.whatsapp.net", "fromMe": false, "id": "IMG1" },
                        "message": { "imageMessage": { "mimetype": "image/jpeg" } }, "messageType": "imageMessage" } }
            """;

        EvolutionWebhookParser.Parse(json).Should().BeNull();
    }

    [Fact]
    public void ConnectionUpdate_ReadsStateAndLinkedNumber()
    {
        var json = $$"""
            { "event": "connection.update", "instance": "{{Instance}}",
              "data": { "instance": "{{Instance}}", "wuid": "573001234567@s.whatsapp.net", "profileName": "Tienda",
                        "state": "open", "statusReason": 200 } }
            """;

        EvolutionWebhookParser.Parse(json).Should().BeEquivalentTo(
            new EvolutionConnectionUpdatedEvent(Instance, "open", "573001234567", "Tienda"));
    }

    [Fact]
    public void QrCodeUpdated_ReadsTheQrImage()
    {
        var json = $$"""
            { "event": "qrcode.updated", "instance": "{{Instance}}",
              "data": { "qrcode": { "instance": "{{Instance}}", "pairingCode": null, "code": "2@abc", "base64": "data:image/png;base64,AAA" } } }
            """;

        var parsed = EvolutionWebhookParser.Parse(json).Should().BeOfType<EvolutionQrUpdatedEvent>().Subject;

        parsed.Qr.Should().Be(new EvolutionQrCode("data:image/png;base64,AAA", "2@abc", null));
    }

    [Theory]
    [InlineData("MESSAGES_UPSERT")]
    [InlineData("messages.upsert")]
    public void EventNames_AreAcceptedInBothFormats(string eventName)
    {
        var json = Message("573001234567@s.whatsapp.net").Replace("\"messages.upsert\"", $"\"{eventName}\"");

        EvolutionWebhookParser.Parse(json).Should().BeOfType<EvolutionTextMessageEvent>();
    }

    [Theory]
    [InlineData("""{ "event": "presence.update", "instance": "ocap-00000000000000000000000000000001", "data": {} }""")]
    [InlineData("""{ "event": "messages.upsert", "data": {} }""")]
    [InlineData("[]")]
    public void UnsupportedOrIncompleteEvents_AreIgnored(string json)
    {
        EvolutionWebhookParser.Parse(json).Should().BeNull();
    }
}
