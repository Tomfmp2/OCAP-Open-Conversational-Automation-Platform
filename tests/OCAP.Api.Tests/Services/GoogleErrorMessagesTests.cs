using FluentAssertions;
using OCAP.Api.Services;

namespace OCAP.Api.Tests.Services;

public class GoogleErrorMessagesTests
{
    [Fact]
    public void ADisabledGoogleApi_TellsThePersonToEnableIt_NotToCheckTheAiModel()
    {
        var error = new HttpRequestException(
            "Google Calendar API respondió 403 (Forbidden): { \"error\": { \"message\": \"Google Calendar API has not been used in project 123 before or it is disabled.\" } }");

        var message = GoogleErrorMessages.Describe(error);

        message.Should().Contain("Google Calendar no está activado").And.Contain("Google Cloud");
        message.Should().NotContain("123", "no se muestra la respuesta cruda de Google");
    }

    [Theory]
    [InlineData("Gmail API respondió 401 (Unauthorized): {}", "Vuelve a conectar Google")]
    [InlineData("Google Sheets API respondió 403 (Forbidden): insufficient authentication scopes", "Vuelve a conectar Google")]
    [InlineData("Google Calendar API respondió 429 (Too Many Requests): quota", "demasiadas peticiones")]
    [InlineData("Google Calendar API respondió 400 (Bad Request): {}", "código 400")]
    public void OtherGoogleFailures_AreExplained(string raw, string expected) =>
        GoogleErrorMessages.Describe(new HttpRequestException(raw)).Should().Contain(expected);

    [Fact]
    public void ErrorsThatAreNotFromGoogle_AreLeftToTheGeneralHandling() =>
        GoogleErrorMessages.Describe(new InvalidOperationException("quota exceeded for model")).Should().BeNull();
}
