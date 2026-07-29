using System.Text.Json;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

public class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public T Deserialize<T>(string payload)
    {
        return JsonSerializer.Deserialize<T>(payload, _options)!;
    }

    public object? Deserialize(string payload, Type type)
    {
        return JsonSerializer.Deserialize(payload, type, _options);
    }
}
