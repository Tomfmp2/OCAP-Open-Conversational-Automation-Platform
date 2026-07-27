using FluentAssertions;
using OCAP.Providers.Google.Calendar;
using OCAP.Tools.Abstractions;
using OCAP.Tools.Google;

namespace OCAP.Tools.Tests;

public class CalendarToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidParameters_CreatesCalendarEvent()
    {
        // Arrange
        var provider = new InMemoryCalendarProvider();
        var tool = new CreateCalendarEventTool(provider);

        var parameters = new Dictionary<string, object>
        {
            ["Title"] = "Reunión de Planificación",
            ["Description"] = "Reunión estratégica de la plataforma OCAP",
            ["StartDate"] = DateTime.UtcNow.AddHours(2).ToString("o"),
            ["EndDate"] = DateTime.UtcNow.AddHours(3).ToString("o"),
            ["Attendees"] = new List<string> { "dev@ocap.org" }
        };

        var context = new ToolExecutionContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), parameters);

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("éxito");

        var events = await provider.GetEventsAsync(DateTime.MinValue, DateTime.MaxValue);
        events.Should().HaveCount(1);
        events.First().Title.Should().Be("Reunión de Planificación");
    }
}
