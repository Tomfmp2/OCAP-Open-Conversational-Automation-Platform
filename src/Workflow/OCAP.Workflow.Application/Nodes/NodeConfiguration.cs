using System.Text.Json;

namespace OCAP.Workflow.Application.Nodes;

public static class NodeConfiguration
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T Deserialize<T>(string? configurationJson) where T : new()
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            return new T();

        try
        {
            return JsonSerializer.Deserialize<T>(configurationJson, JsonOptions) ?? new T();
        }
        catch (JsonException)
        {
            return new T();
        }
    }
}
