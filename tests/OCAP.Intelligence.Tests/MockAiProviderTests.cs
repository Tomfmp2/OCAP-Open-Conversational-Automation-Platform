using FluentAssertions;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Mock;

namespace OCAP.Intelligence.Tests;

public class MockAiProviderTests
{
    private readonly MockAiProvider _provider = new();

    [Fact]
    public async Task GenerateResponse_WithMeetingKeyword_ReturnsMeetingResponse()
    {
        // Arrange
        var request = new AiRequest { UserMessage = "Necesito agendar una reunión mañana a las 10am" };

        // Act
        var response = await _provider.GenerateResponseAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.GeneratedText.Should().Contain("agendar la reunión");
        response.ProviderName.Should().Be("MockAI");
    }

    [Fact]
    public async Task AnalyzeIntent_WithMeetingKeyword_ResolvesCreateReminderIntent()
    {
        // Act
        var intent = await _provider.AnalyzeIntentAsync("Agendar cita con cliente");

        // Assert
        intent.Should().NotBeNull();
        intent.Name.Should().Be("CreateReminder");
        intent.Confidence.Should().BeGreaterThan(0.9f);
    }

    [Fact]
    public void GetModelInformation_ReturnsValidMetadata()
    {
        // Act
        var info = _provider.GetModelInformation();

        // Assert
        info.Should().NotBeNull();
        info.Provider.Should().Be("MockAI");
        info.Model.Should().Be("mock-gpt-4o");
        info.Capabilities.Should().Contain("text-generation");
    }
}
