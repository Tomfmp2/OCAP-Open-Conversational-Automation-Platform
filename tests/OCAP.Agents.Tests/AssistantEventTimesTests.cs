using FluentAssertions;
using OCAP.Agents.Application.Services;

namespace OCAP.Agents.Tests;

public class AssistantEventTimesTests
{
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    [Fact]
    public void NineInTheMorning_InBogota_IsStoredAsTwoInTheAfternoonUtc()
    {
        var args = new Dictionary<string, object> { ["Title"] = "Cita", ["StartDate"] = "2026-11-13T09:00:00" };

        var local = AssistantEventTimes.NormalizeToUtc(args, Bogota);

        args["StartDate"].Should().Be("2026-11-13T14:00:00Z");
        local.Should().Be(new DateTime(2026, 11, 13, 9, 0, 0));
    }

    [Fact]
    public void ATimeThatAlreadyCarriesItsZone_IsRespected()
    {
        var args = new Dictionary<string, object> { ["StartDate"] = "2026-11-13T09:00:00-05:00", ["EndDate"] = "2026-11-13T15:30:00Z" };

        AssistantEventTimes.NormalizeToUtc(args, Bogota);

        args["StartDate"].Should().Be("2026-11-13T14:00:00Z");
        args["EndDate"].Should().Be("2026-11-13T15:30:00Z");
    }

    [Theory]
    [InlineData("mañana a las nueve")]
    [InlineData("")]
    public void AStartThatCannotBeRead_IsNotGuessed(string start)
    {
        var args = new Dictionary<string, object> { ["StartDate"] = start };

        AssistantEventTimes.NormalizeToUtc(args, Bogota).Should().BeNull();
    }

    [Fact]
    public void AnUnreadableEnd_IsDropped_SoTheEventKeepsItsDefaultLength()
    {
        var args = new Dictionary<string, object> { ["StartDate"] = "2026-11-13T09:00:00", ["EndDate"] = "luego" };

        AssistantEventTimes.NormalizeToUtc(args, Bogota).Should().NotBeNull();
        args.Should().NotContainKey("EndDate");
    }
}
