using OCAP.Core.Events;
using OCAP.Infrastructure.Events.Distributed;
using Xunit;

namespace OCAP.UnitTests.Events;

public class JsonEventSerializerTests
{
    [Fact]
    public void AnEventWithAShortConstructor_RoundTripsKeepingItsIdAndTime()
    {
        // Antes fallaba en cada mensaje de canal: dos constructores públicos y System.Text.Json no elegía ninguno.
        var serializer = new JsonEventSerializer();
        var original = new MessageReceivedEvent("Telegram", "8577661760", "Hola", Guid.NewGuid());

        var copy = (MessageReceivedEvent)serializer.Deserialize(serializer.Serialize(original), typeof(MessageReceivedEvent))!;

        Assert.Equal(original, copy);
    }
}
