using System.Text.Json;

namespace OCAP.Agents.Application.Services;

/// <summary>Plan que devuelve el modelo: texto para el usuario y, opcionalmente, una herramienta a ejecutar.</summary>
public sealed record AssistantPlan(string Reply, string? Tool, Dictionary<string, object> Args)
{
    public bool HasTool => !string.IsNullOrWhiteSpace(Tool) && !string.Equals(Tool, "null", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Lee el JSON <c>{"reply","tool","args"}</c> que piden los prompts de los agentes, tolerando markdown alrededor.</summary>
public static class AssistantPlanParser
{
    public static AssistantPlan? TryParse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var text = content.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                text = text[start..(end + 1)];
            }
        }

        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return null;
        }

        text = text[jsonStart..(jsonEnd + 1)];

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var reply = root.TryGetProperty("reply", out var r) ? r.GetString() ?? string.Empty : string.Empty;
            string? tool = null;
            if (root.TryGetProperty("tool", out var t))
            {
                tool = t.ValueKind == JsonValueKind.Null ? null : t.GetString();
            }

            var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("args", out var a) && a.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in a.EnumerateObject())
                {
                    args[prop.Name] = ConvertJsonElement(prop.Value);
                }
            }

            return new AssistantPlan(reply, tool, args);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static object ConvertJsonElement(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => el.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => el.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => el.ToString()
        };
}
